using OneOf;
using Synology.Api.Sdk.SynologyApi.FileStation.Response;
using Synology.Photos.Slideshow.Api.Slideshow.Common.Errors;

namespace Synology.Photos.Slideshow.Api.Slideshow.DownloadPhotos.Services;

public interface IFileStation
{
    Task Download(IList<FileStationItem> fileStationItems, CancellationToken cancellationToken);
}