using Synology.Photos.Slideshow.Api.Slideshow.Services;

namespace Synology.Photos.Slideshow.Api.Slideshow.Endpoints;

public static class Thumbnails
{
    public static async Task<IResult> GetAsync(IPhotosService photosService, CancellationToken cancellationToken)
    {
        var thumbnails = await photosService.GetThumbnails(cancellationToken);

        return Results.Ok(thumbnails);
    }
}
