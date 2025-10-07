namespace Synology.Photos.Slideshow.Api.Slideshow.Web.Services;

public interface IPhotosService
{
    Task<IReadOnlyList<string>> GetPhotoRelativeUrls(CancellationToken cancellationToken);
}