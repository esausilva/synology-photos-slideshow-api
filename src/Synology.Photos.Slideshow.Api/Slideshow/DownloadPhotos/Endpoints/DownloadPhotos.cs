using Synology.Photos.Slideshow.Api.Slideshow.DownloadPhotos.Services;

namespace Synology.Photos.Slideshow.Api.Slideshow.DownloadPhotos.Endpoints;

public static class DownloadPhotos
{
    public static async Task<IResult> GetAsync(
        HttpContext context,
        INasPhotoSearchService photoSearchService,
        IFileStation fileStation,
        IFileProcessor fileProcessor,
        CancellationToken cancellationToken)
    {
        var fileStationItemsResult = await photoSearchService.SearchPhotos(cancellationToken);
        
        if (fileStationItemsResult.IsFailed)
            return CreateProblemResult(fileStationItemsResult.Errors.Single().Message);

        await fileStation.Download(fileStationItemsResult.Value, cancellationToken);
        await fileProcessor.ProcessZipFile(cancellationToken);

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
