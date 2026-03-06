namespace Synology.Photos.Slideshow.Api.Slideshow.Services;

public interface IFileProcessor
{
    Task CleanDownloadDirectory(CancellationToken cancellationToken);
    Task ProcessZipFile(CancellationToken cancellationToken);
    Task DeletePhotos(IList<string> photosToDelete, CancellationToken cancellationToken);
}