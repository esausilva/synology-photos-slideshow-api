using Microsoft.AspNetCore.SignalR;
using Synology.Photos.Slideshow.Api.Slideshow.Hubs;
using Synology.Photos.Slideshow.Api.Slideshow.Services;

namespace Synology.Photos.Slideshow.Api.Slideshow.Jobs;

public class PhotoDownloadJob
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IHubContext<SlideshowHub, ISlideshowHub> _hubContext;
    private readonly ILogger<PhotoDownloadJob> _logger;

    public PhotoDownloadJob(
        IServiceScopeFactory serviceScopeFactory,
        IHubContext<SlideshowHub, ISlideshowHub> hubContext,
        ILogger<PhotoDownloadJob> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Execute(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting scheduled photo download job");

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            
            var photoSearchService = scope.ServiceProvider.GetRequiredService<INasPhotoSearchService>();
            var fileStation = scope.ServiceProvider.GetRequiredService<IFileStation>();
            var fileProcessor = scope.ServiceProvider.GetRequiredService<IFileProcessor>();
            var photoService = scope.ServiceProvider.GetRequiredService<IPhotosService>();

            var fileStationItemsResult = await photoSearchService.SearchPhotos(cancellationToken);

            if (fileStationItemsResult.IsFailed)
            {
                var errorMessage = fileStationItemsResult.Errors.Single().Message;
                await _hubContext.Clients.All.PhotoProcessingError($"Failed to search photos: {errorMessage}");
                
                return;
            }

            await fileStation.Download(fileStationItemsResult.Value, cancellationToken);
            await fileProcessor.ProcessZipFile(cancellationToken);
            await photoService.ProcessPhotos(cancellationToken);

            _logger.LogInformation("Photo download job completed successfully. Sending refresh signal.");
            await _hubContext.Clients.All.RefreshSlideshow();
        }
        catch (Exception ex)
        {
            const string errorMessage = "An error occurred during scheduled photo download";
            _logger.LogError(ex, errorMessage);
            await _hubContext.Clients.All.PhotoProcessingError(errorMessage);
        }
    }
}