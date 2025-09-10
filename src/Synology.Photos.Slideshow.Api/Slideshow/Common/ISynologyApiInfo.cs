using Synology.Api.Sdk.SynologyApi.ApiInfo.Response;

namespace Synology.Photos.Slideshow.Api.Slideshow.Common;

public interface ISynologyApiInfo
{
    Task<ApiInfoData> GetApiInfo(CancellationToken cancellationToken);
}