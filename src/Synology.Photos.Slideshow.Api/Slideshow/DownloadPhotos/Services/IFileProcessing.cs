namespace Synology.Photos.Slideshow.Api.Slideshow.DownloadPhotos.Services;

public interface IFileProcessing
{
    Task CleanDownloadDirectory(CancellationToken cancellationToken);
    Task ProcessZipFile(CancellationToken cancellationToken);
}