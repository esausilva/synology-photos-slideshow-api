using Synology.Photos.Slideshow.Api.Slideshow.Services;

namespace Synology.Photos.Slideshow.Api.Slideshow.Endpoints;

public static class DownloadPhotos
{
    /*
     * NOTE: This is a process-intensive endpoint as it searches the Synology NAS, downloads the photos,
     * processes them into a flattened hierarchy, and then converts them to WebP format. I will break
     * these up later into background services after implementing client real-time notifications.
     */
    public static async Task<IResult> GetAsync(
        HttpContext context,
        INasPhotoSearchService photoSearchService,
        IFileStation fileStation,
        IFileProcessor fileProcessor,
        IPhotosService photosService,
        CancellationToken cancellationToken)
    {
        var fileStationItemsResult = await photoSearchService.SearchPhotos(cancellationToken);

        if (fileStationItemsResult.IsFailed)
            return CreateProblemResult(fileStationItemsResult.Errors.Single().Message);

        await fileStation.Download(fileStationItemsResult.Value, cancellationToken);
        await fileProcessor.ProcessZipFile(cancellationToken);
        await photosService.ProcessPhotos(cancellationToken);

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