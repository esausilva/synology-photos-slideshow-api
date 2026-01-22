using Synology.Api.Sdk.SynologyApi.ApiInfo.Response;

namespace Synology.Photos.Slideshow.Api.Slideshow.Providers;

public interface ISynologyApiInfoProvider
{
    Task<ApiInfoData> GetOrFetchInfo(CancellationToken cancellationToken);
}