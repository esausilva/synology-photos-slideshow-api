namespace Synology.Photos.Slideshow.Api.Slideshow.DownloadPhotos.Services;

public interface ISynologyApiSearch
{
    Task<IEnumerable<string>> GetPhotos(CancellationToken cancellationToken);
}