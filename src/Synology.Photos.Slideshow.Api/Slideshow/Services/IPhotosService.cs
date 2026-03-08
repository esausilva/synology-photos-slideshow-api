using Synology.Photos.Slideshow.Api.Slideshow.Endpoints.Response;

namespace Synology.Photos.Slideshow.Api.Slideshow.Services;

public interface IPhotosService
{
    Task ProcessPhotos(CancellationToken cancellationToken);
    Task<IReadOnlyList<SlideResponse>> GetSlides(CancellationToken cancellationToken);
}