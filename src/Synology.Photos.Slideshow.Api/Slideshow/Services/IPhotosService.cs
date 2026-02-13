using Synology.Photos.Slideshow.Api.Slideshow.Response;

namespace Synology.Photos.Slideshow.Api.Slideshow.Services;

public interface IPhotosService
{
    Task ProcessPhotos(CancellationToken cancellationToken);
    Task<IReadOnlyList<SlidesResponse>> GetSlides(CancellationToken cancellationToken);
}