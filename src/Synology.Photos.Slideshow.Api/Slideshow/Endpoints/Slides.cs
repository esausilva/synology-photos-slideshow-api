using Synology.Photos.Slideshow.Api.Slideshow.Services;

namespace Synology.Photos.Slideshow.Api.Slideshow.Endpoints;

public static class Slides
{
    public static async Task<IResult> GetAsync(IPhotosService photosService, CancellationToken cancellationToken)
    {
        var photoUrls = await photosService.GetPhotoRelativeUrls(cancellationToken);
        
        return Results.Ok(photoUrls);
    }
}