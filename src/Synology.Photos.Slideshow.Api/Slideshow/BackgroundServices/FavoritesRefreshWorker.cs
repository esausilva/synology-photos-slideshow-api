using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Synology.Photos.Slideshow.Api.Configuration;
using Synology.Photos.Slideshow.Api.Slideshow.Hubs;
using Synology.Photos.Slideshow.Api.Slideshow.Messaging;

namespace Synology.Photos.Slideshow.Api.Slideshow.BackgroundServices;

public sealed partial class FavoritesRefreshWorker : BackgroundService
{
    private readonly IFavoritesRefreshSignalChannel _signalChannel;
    private readonly IOptionsMonitor<FavoritesWatcherOptions> _watcherOptions;
    private readonly IHubContext<SlideshowHub, ISlideshowHub> _hubContext;
    private readonly ILogger<FavoritesRefreshWorker> _logger;

    public FavoritesRefreshWorker(
        IFavoritesRefreshSignalChannel signalChannel,
        IOptionsMonitor<FavoritesWatcherOptions> watcherOptions,
        IHubContext<SlideshowHub, ISlideshowHub> hubContext,
        ILogger<FavoritesRefreshWorker> logger)
    {
        _signalChannel = signalChannel;
        _watcherOptions = watcherOptions;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Consumes favorite toggle signals and notifies clients to refresh. Uses the same debounce mechanism as
    /// <see cref="FavoritesFolderWatcherWorker"/>: every new signal resets the timer, so consecutive toggles
    /// result in a single refresh once a period of silence is reached.
    /// </summary>
    /// <param name="stoppingToken">Triggered when the application host is performing a graceful shutdown.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FavoritesRefreshWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 1. Wait for a signal that a favorite was toggled
                if (!await _signalChannel.WaitToReadAsync(stoppingToken))
                    break;

                var senderConnectionIds = new HashSet<string>();
                DrainSignals(senderConnectionIds);

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
                        if (!await _signalChannel.WaitToReadAsync(timeoutCts.Token))
                            break;

                        DrainSignals(senderConnectionIds);
                        _logger.LogDebug("New refresh signal detected during debounce. Resetting timer.");
                    }
                    catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                    {
                        // Timeout reached, we have silence
                        break;
                    }
                }

                if (stoppingToken.IsCancellationRequested)
                    break;

                // 3. Notify clients
                _logger.LogInformation("Debounce timer expired. Triggering gallery and slideshow refresh.");
                await NotifyClients(senderConnectionIds);
            }
            catch (OperationCanceledException)
            {
                // Stopping token triggered
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FavoritesRefreshWorker");
            }
        }

        _logger.LogInformation("FavoritesRefreshWorker stopped");
    }

    private void DrainSignals(HashSet<string> senderConnectionIds)
    {
        while (_signalChannel.TryRead(out var connectionId))
        {
            if (!string.IsNullOrWhiteSpace(connectionId))
                senderConnectionIds.Add(connectionId);
        }
    }

    /// <summary>
    /// Excludes the sender only when a single client toggled favorites during the debounce window.
    /// With multiple senders, each one needs to see the others' changes, so every client is refreshed.
    /// </summary>
    private async Task NotifyClients(HashSet<string> senderConnectionIds)
    {
        var clients = senderConnectionIds.Count == 1
            ? _hubContext.Clients.AllExcept(senderConnectionIds.First())
            : _hubContext.Clients.All;

        await clients.RefreshSlideshow();
        await clients.RefreshGallery();
    }

    [LoggerMessage(LogLevel.Debug, "Refresh signal detected. Waiting {Delay} seconds for silence...")]
    partial void LogChangeDetectedWaitingDelaySecondsForSilence(double delay);
}
