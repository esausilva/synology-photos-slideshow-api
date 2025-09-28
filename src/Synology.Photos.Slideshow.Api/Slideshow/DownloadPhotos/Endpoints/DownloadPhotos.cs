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

        if(fileStationItemsResult.TryPickT0(out var fileStationItems, out _))
            await fileStation.Download(fileStationItems, cancellationToken);
        
        await fileProcessing.ProcessZipFile(cancellationToken);
        
        return fileStationItemsResult.Match(
            // TODO: Return NoContent when finalized, but returning Photo Paths for now 
            items => Results.Ok(items.Select(p => p.Path).ToList()),
            
            invalidApiVersionError => CreateProblemResult("Invalid API Version", invalidApiVersionError.Message),
            searchError => CreateProblemResult("Search Failed", searchError.Message),
            timeOutError => CreateProblemResult("Search Timeout", timeOutError.Message));
    }
    
    private static IResult CreateProblemResult(string title, string detail)
    {
        return Results.Problem(
            type: "https://datatracker.ietf.org/doc/html/rfc9110#status.400",
            statusCode: StatusCodes.Status400BadRequest,
            title: title,
            detail: detail);
    } 
}
