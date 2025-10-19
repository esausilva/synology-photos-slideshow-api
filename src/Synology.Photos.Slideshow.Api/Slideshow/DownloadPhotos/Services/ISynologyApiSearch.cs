using FluentResults;
using Synology.Api.Sdk.SynologyApi.FileStation.Response;

namespace Synology.Photos.Slideshow.Api.Slideshow.DownloadPhotos.Services;

public interface ISynologyApiSearch
{
    Task<Result<IList<FileStationItem>>> GetPhotos(CancellationToken cancellationToken);
}