using Microsoft.AspNetCore.SignalR;
using Polly;
using Polly.Retry;
using Synology.Photos.Slideshow.Api.Slideshow.Hubs;
using Synology.Photos.Slideshow.Api.Slideshow.Messaging;
using Synology.Photos.Slideshow.Api.Slideshow.Services;

namespace Synology.Photos.Slideshow.Api.Slideshow.BackgroundServices;

public sealed class ThumbnailProcessingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPhotoThumbnailProcessingChannel _thumbnailProcessingChannel;
    private readonly IHubContext<SlideshowHub, ISlideshowHub> _hubContext;
    private readonly ILogger<ThumbnailProcessingWorker> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;

    private const int MaxRetryAttempts = 3;

    public ThumbnailProcessingWorker(
        IServiceScopeFactory scopeFactory,
        IPhotoThumbnailProcessingChannel thumbnailProcessingChannel,
        IHubContext<SlideshowHub, ISlideshowHub> hubContext,
        ILogger<ThumbnailProcessingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _thumbnailProcessingChannel = thumbnailProcessingChannel;
        _hubContext = hubContext;
        _logger = logger;
        _resiliencePipeline = CreateResiliencePipeline();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ThumbnailProcessingWorker started, waiting for messages...");

        await foreach (var _ in _thumbnailProcessingChannel.ReadAllAsync(stoppingToken))
        {
            _logger.LogInformation("Received thumbnail processing request");

            try
            {
                await _resiliencePipeline.ExecuteAsync(async ct =>
                {
                    using var scope = _scopeFactory.CreateScope();
                    var photoService = scope.ServiceProvider.GetRequiredService<IPhotosService>();

                    await photoService.CreateThumbnails(ct);
                }, stoppingToken);

                _logger.LogInformation("Thumbnail processing completed successfully. Sending a refresh signal.");

                await _hubContext.Clients.All.RefreshGallery();
            }
            catch (Exception ex)
            {
                const string errorMessage = "An error occurred while creating thumbnails";

                _logger.LogError(ex, errorMessage);

                await _hubContext.Clients.All.ThumbnailsProcessingError(errorMessage);
            }
        }

        _logger.LogInformation("ThumbnailProcessingWorker stopped");
    }

    private ResiliencePipeline CreateResiliencePipeline()
    {
        var delay = TimeSpan.FromSeconds(2);

        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = MaxRetryAttempts,
                Delay = delay,
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    _logger.LogWarning(args.Outcome.Exception,
                        "Thumbnail processing failed on attempt {AttemptNumber} of {MaxRetryAttempts}. Retrying in {RetryDelay} seconds",
                        args.AttemptNumber + 1,
                        MaxRetryAttempts,
                        args.RetryDelay.TotalSeconds);

                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }
}
