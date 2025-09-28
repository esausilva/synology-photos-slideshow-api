using OneOf;
using Synology.Api.Sdk.SynologyApi.FileStation.Response;
using Synology.Photos.Slideshow.Api.Slideshow.Common.Errors;

namespace Synology.Photos.Slideshow.Api.Slideshow.DownloadPhotos.Services;

public interface ISynologyApiSearch
{
    Task<OneOf<IList<FileStationItem>, InvalidApiVersionError, FailedToInitiateSearchError, SearchTimedOutError>> GetPhotos(CancellationToken cancellationToken);
}