using Synology.Photos.Slideshow.Api.Slideshow.Web.Services;

namespace Synology.Photos.Slideshow.Api.Slideshow.Web.Endpoints;

public static class Slides
{
    public static async Task<IResult> GetAsync(IPhotosService photosService, CancellationToken cancellationToken)
    {
        var photoUrls = await photosService.GetPhotoRelativeUrls(cancellationToken);
        
        return Results.Ok(photoUrls);
    }
}