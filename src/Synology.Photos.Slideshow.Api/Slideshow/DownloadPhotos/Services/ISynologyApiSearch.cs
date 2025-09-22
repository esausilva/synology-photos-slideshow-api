using OneOf;
using Synology.Photos.Slideshow.Api.Slideshow.Common.Errors;

namespace Synology.Photos.Slideshow.Api.Slideshow.DownloadPhotos.Services;

public interface ISynologyApiSearch
{
    Task<OneOf<IEnumerable<string>, InvalidApiVersionError, FailedToInitiateSearchError, SearchTimedOutError>> GetPhotos(CancellationToken cancellationToken);
}