using Synology.Photos.Slideshow.Api.Slideshow.DownloadPhotos.Services;

namespace Synology.Photos.Slideshow.Api.Slideshow.DownloadPhotos.Endpoints;

public static class DownloadPhotos
{
    public static async Task<IResult> GetAsync(
        HttpContext context,
        ISynologyApiSearch synoApiSearch,
        CancellationToken cancellationToken)
    {
        var photoPathsResult = await synoApiSearch.GetPhotos(cancellationToken);

        return photoPathsResult.Match(
            photoPaths => Results.Ok(photoPaths),
            invalidApiVersionError => Results.BadRequest(invalidApiVersionError.Message),
            searchError => Results.BadRequest(searchError.Message),
            timeOutError => Results.BadRequest(timeOutError.Message));
    }
}
