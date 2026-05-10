using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Synology.Photos.Slideshow.Api.Configuration;
using Synology.Photos.Slideshow.Api.Constants;
using Synology.Photos.Slideshow.Api.Slideshow.Hubs;
using Synology.Photos.Slideshow.Api.Slideshow.Services;

namespace Synology.Photos.Slideshow.Api.Slideshow.BackgroundServices;

public sealed partial class FavoritesFolderWatcherWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<SynoApiOptions> _synoApiOptions;
    private readonly IOptionsMonitor<FavoritesWatcherOptions> _watcherOptions;
    private readonly IHubContext<SlideshowHub, ISlideshowHub> _hubContext;
    private readonly ILogger<FavoritesFolderWatcherWorker> _logger;
    
    private FileSystemWatcher? _watcher;
    private readonly Channel<byte> _signalChannel;

    public FavoritesFolderWatcherWorker(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<SynoApiOptions> synoApiOptions,
        IOptionsMonitor<FavoritesWatcherOptions> watcherOptions,
        IHubContext<SlideshowHub, ISlideshowHub> hubContext,
        ILogger<FavoritesFolderWatcherWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _synoApiOptions = synoApiOptions;
        _watcherOptions = watcherOptions;
        _hubContext = hubContext;
        _logger = logger;

        // Bounded channel of 1 ensures we only keep a single signal. 
        // If processing is busy, new signals just overwrite the existing one.
        _signalChannel = Channel.CreateBounded<byte>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });
    }

    /// <summary>
    /// Starts the background monitoring process for the "favorites" folder.
    /// This worker initializes a <see cref="FileSystemWatcher"/> and uses a channel-based consumer loop
    /// to process file events sequentially. It implements a debounced mechanism to handle batch uploads
    /// without interrupting active processing or causing cancellation exceptions.
    /// </summary>
    /// <param name="stoppingToken">Triggered when the application host is performing a graceful shutdown.</param>
    /// <returns>A <see cref="Task"/> representing the background operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var favoritesPath = Path.Combine(_synoApiOptions.CurrentValue.DownloadAbsolutePath, SlideshowConstants.FavoritesFolderName);

        if (!Directory.Exists(favoritesPath))
        {
            LogCreatingFavoritesDirectoryAtPath(favoritesPath);
            Directory.CreateDirectory(favoritesPath);
        }

        LogStartingFavoritesFolderWatcherAtPath(favoritesPath);

        _watcher = new FileSystemWatcher(favoritesPath)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
            Filter = "*.*",
            EnableRaisingEvents = true
        };

        _watcher.Created += OnFileEvent;

        // Main Consumer Loop
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 1. Wait for a signal that something changed
                if (!await _signalChannel.Reader.WaitToReadAsync(stoppingToken))
                    break;

                // Drain the signal
                _signalChannel.Reader.TryRead(out _);

                // 2. Debounce: Wait for a period of silence
                var debounceDelay = TimeSpan.FromSeconds(_watcherOptions.CurrentValue.DebounceDelayInSeconds);
                LogChangeDetectedWaitingDelaySecondsForSilence(debounceDelay.TotalSeconds);

                while (!stoppingToken.IsCancellationRequested)
                {
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    timeoutCts.CancelAfter(debounceDelay);

                    try
                    {
                        // If another signal arrives during the delay, we reset the timer
                        if (!await _signalChannel.Reader.WaitToReadAsync(timeoutCts.Token)) 
                            continue;
                        
                        _signalChannel.Reader.TryRead(out _);
                        _logger.LogDebug("New file event detected during debounce. Resetting timer.");
                    }
                    catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                    {
                        // Timeout reached, we have silence
                        break;
                    }
                }

                if (stoppingToken.IsCancellationRequested)
                    break;

                // 3. Process the changes sequentially
                _logger.LogInformation("Debounce timer expired or silence reached. Triggering favorites processing.");
                await ProcessFavorites(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Stopping token triggered
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in favorites watcher consumer loop");
            }
        }

        _logger.LogInformation("Favorites folder watcher stopping");
    }

    private void OnFileEvent(object sender, FileSystemEventArgs e)
    {
        if (!ShouldProcessFile(e.Name))
            return;

        LogFileEventTypeDetectedPath(e.ChangeType, e.Name!);
        _signalChannel.Writer.TryWrite(0);
    }

    private static bool ShouldProcessFile(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var extension = Path.GetExtension(fileName);
        return SlideshowConstants.ImageExtensionsForConversion.Contains(extension);
    }

    private async Task ProcessFavorites(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var photosService = scope.ServiceProvider.GetRequiredService<IPhotosService>();
        var favoritesPath = Path.Combine(_synoApiOptions.CurrentValue.DownloadAbsolutePath, SlideshowConstants.FavoritesFolderName);

        try
        {
            LogProcessingFavoritesInPath(favoritesPath);
            
            await photosService.ProcessPhotos(favoritesPath, ct);
            await photosService.CreateThumbnails(favoritesPath, ct);

            _logger.LogInformation("Favorites processing completed successfully. Notifying clients.");
            await _hubContext.Clients.All.RefreshSlideshow();
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Favorites processing was cancelled due to application shutdown.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process favorites folder");
            await _hubContext.Clients.All.PhotoProcessingError("Failed to process favorites folder");
        }
    }

    public override void Dispose()
    {
        if (_watcher != null)
        {
            _watcher.Created -= OnFileEvent;
            _watcher.Dispose();
        }
        
        base.Dispose();
    }

    [LoggerMessage(LogLevel.Information, "Creating favorites directory at {Path}")]
    partial void LogCreatingFavoritesDirectoryAtPath(string path);

    [LoggerMessage(LogLevel.Information, "Starting favorites folder watcher at {Path}")]
    partial void LogStartingFavoritesFolderWatcherAtPath(string path);

    [LoggerMessage(LogLevel.Debug, "Change detected. Waiting {Delay} seconds for silence...")]
    partial void LogChangeDetectedWaitingDelaySecondsForSilence(double delay);

    [LoggerMessage(LogLevel.Information, "Processing favorites in {Path}")]
    partial void LogProcessingFavoritesInPath(string path);

    [LoggerMessage(LogLevel.Debug, "File {EventType} detected: {Path}")]
    partial void LogFileEventTypeDetectedPath(WatcherChangeTypes eventType, string path);
}
