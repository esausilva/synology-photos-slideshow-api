using Synology.Photos.Slideshow.Api.Slideshow.Services;

namespace Synology.Photos.Slideshow.Api.Slideshow.Endpoints;

public static class DownloadPhotos
{
    /*
     * NOTE: This is a process-intensive endpoint. I will break it up into background services later
     * after implementing real-time client notifications.
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