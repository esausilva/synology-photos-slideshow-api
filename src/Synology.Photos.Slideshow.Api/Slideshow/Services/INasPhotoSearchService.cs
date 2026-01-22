using FluentResults;
using Synology.Api.Sdk.SynologyApi.FileStation.Response;

namespace Synology.Photos.Slideshow.Api.Slideshow.Services;

public interface INasPhotoSearchService
{
    Task<Result<IList<FileStationItem>>> SearchPhotos(CancellationToken cancellationToken);
}