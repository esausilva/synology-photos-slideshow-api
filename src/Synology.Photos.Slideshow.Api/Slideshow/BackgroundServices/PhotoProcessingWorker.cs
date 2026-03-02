using Polly;
using Polly.Retry;
using Synology.Photos.Slideshow.Api.Slideshow.Messaging;
using Synology.Photos.Slideshow.Api.Slideshow.Services;

namespace Synology.Photos.Slideshow.Api.Slideshow.BackgroundServices;

public sealed class PhotoProcessingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPhotoProcessingChannel _photoProcessingChannel;
    private readonly ILogger<PhotoProcessingWorker> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;
    
    private const int MaxRetryAttempts = 3;
    
    public PhotoProcessingWorker(
        IServiceScopeFactory scopeFactory,
        IPhotoProcessingChannel photoProcessingChannel,
        ILogger<PhotoProcessingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _photoProcessingChannel = photoProcessingChannel;
        _logger = logger;
        _resiliencePipeline = CreateResiliencePipeline();
    }

    /// <summary>
    /// Executes the main process of the PhotoProcessingService, handling incoming photo processing requests
    /// and processing them asynchronously while managing resilience and error handling.
    /// </summary>
    /// <param name="stoppingToken">
    /// A <see cref="CancellationToken"/> that is triggered when the service is stopping,
    /// allowing the method to terminate ongoing operations gracefully.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous execution of the service's processing loop.
    /// </returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PhotoProcessingService started, waiting for messages...");
        
        await foreach (var _ in _photoProcessingChannel.ReadAllAsync(stoppingToken))
        {
            _logger.LogInformation("Received photo processing request");

            try
            {
                await _resiliencePipeline.ExecuteAsync(async ct =>
                {
                    using var scope = _scopeFactory.CreateScope();
                    var photoService = scope.ServiceProvider.GetRequiredService<IPhotosService>();

                    await photoService.ProcessPhotos(ct);
                }, stoppingToken);

                _logger.LogInformation("Photo processing completed successfully");
                
                // TODO: Once SignalR is implemented, send a success message to the client to refresh the slideshow
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing photos");
                
                // TODO: Once SignalR is implemented, send an error message to the client
            }
        }

        _logger.LogInformation("PhotoProcessingService stopped");
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
                        "Photo processing failed on attempt {AttemptNumber} of {MaxRetryAttempts}. Retrying in {RetryDelay} seconds",
                        args.AttemptNumber + 1, // 0-based attempt number
                        MaxRetryAttempts,
                        args.RetryDelay.TotalSeconds);

                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }
}