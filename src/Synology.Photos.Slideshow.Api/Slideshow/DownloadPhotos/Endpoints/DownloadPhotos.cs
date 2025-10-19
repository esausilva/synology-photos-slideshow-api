using Synology.Photos.Slideshow.Api.Slideshow.DownloadPhotos.Services;

namespace Synology.Photos.Slideshow.Api.Slideshow.DownloadPhotos.Endpoints;

public static class DownloadPhotos
{
    public static async Task<IResult> GetAsync(
        HttpContext context,
        ISynologyApiSearch synoApiSearch,
        IFileStation fileStation,
        IFileProcessing fileProcessing,
        CancellationToken cancellationToken)
    {
        var fileStationItemsResult = await synoApiSearch.GetPhotos(cancellationToken);
        
        if (fileStationItemsResult.IsFailed)
            return CreateProblemResult(fileStationItemsResult.Errors.Single().Message);

        await fileStation.Download(fileStationItemsResult.Value, cancellationToken);
        await fileProcessing.ProcessZipFile(cancellationToken);

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
