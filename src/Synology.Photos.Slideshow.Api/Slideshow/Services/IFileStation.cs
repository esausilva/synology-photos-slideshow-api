using Synology.Api.Sdk.SynologyApi.FileStation.Response;

namespace Synology.Photos.Slideshow.Api.Slideshow.Services;

public interface IFileStation
{
    Task Download(IList<FileStationItem> fileStationItems, CancellationToken cancellationToken);
}