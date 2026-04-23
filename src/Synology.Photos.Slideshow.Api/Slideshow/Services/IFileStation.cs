using Synology.Api.Sdk.SynologyApi.FileStation.Response;

namespace Synology.Photos.Slideshow.Api.Slideshow.Services;

public interface IFileStation
{
    Task<bool> Download(IList<FileStationItem> fileStationItems, CancellationToken cancellationToken);
    Task<bool> Download(IList<FileStationItem> fileStationItems, string synoToken, CancellationToken cancellationToken);
}