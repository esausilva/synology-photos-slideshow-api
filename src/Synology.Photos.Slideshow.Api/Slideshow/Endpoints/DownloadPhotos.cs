using Synology.Photos.Slideshow.Api.Slideshow.Messaging;
using Synology.Photos.Slideshow.Api.Slideshow.Services;

namespace Synology.Photos.Slideshow.Api.Slideshow.Endpoints;

public static class DownloadPhotos
{
    /*
     * NOTE: This is a process-intensive endpoint as it searches the Synology NAS, downloads the photos,
     * and processes them into a flattened hierarchy.
     */
    public static async Task<IResult> GetAsync(
        HttpContext context,
        INasPhotoSearchService photoSearchService,
        IFileStation fileStation,
        IPhotoProcessingChannel photoProcessingChannel,
        CancellationToken cancellationToken)
    {
        var fileStationItemsResult = await photoSearchService.SearchPhotos(cancellationToken);

        if (fileStationItemsResult.IsFailed)
            return CreateProblemResult(fileStationItemsResult.Errors.Single().Message);

        var downloadSuccess = await fileStation.Download(fileStationItemsResult.Value, cancellationToken);
        
        if (!downloadSuccess)
            return CreateProblemResult("Failed to download photos from Synology. Please check the logs.");
        
        // Triggers background photo processing conversion
        await photoProcessingChannel.PublishAsync(cancellationToken);

        return Results.NoContent();
    }

    private static IResult CreateProblemResult(string detail)
    {
        return Results.Problem(
            type: "https://datatracker.ietf.org/doc/html/rfc9110#status.503",
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Failed to download photos.",
            detail: detail);
    }
}