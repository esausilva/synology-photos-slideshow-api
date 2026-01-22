namespace Synology.Photos.Slideshow.Api.Slideshow.Services;

public interface IPhotosService
{
    Task<IReadOnlyList<string>> GetPhotoRelativeUrls(CancellationToken cancellationToken);
}