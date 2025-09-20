using Synology.Photos.Slideshow.Api.Slideshow.DownloadPhotos.Services;

namespace Synology.Photos.Slideshow.Api.Slideshow.DownloadPhotos.Endpoints;

public static class DownloadPhotos
{
    public static async Task<IResult> GetAsync(
        HttpContext context,
        ISynologyApiSearch synoApiSearch,
        CancellationToken cancellationToken)
    {
        var photoPaths = await synoApiSearch.GetPhotos(cancellationToken);
        
        return Results.Ok(photoPaths);
    }
}
