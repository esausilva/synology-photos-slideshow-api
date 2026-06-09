using Synology.Api.Sdk.Constants;
using Synology.Api.Sdk.SynologyApi;
using Synology.Api.Sdk.SynologyApi.ApiInfo.Request;
using Synology.Api.Sdk.SynologyApi.ApiInfo.Response;

namespace Synology.Photos.Slideshow.Api.Slideshow.Providers;

public sealed class SynologyApiInfoProvider : ISynologyApiInfoProvider
{
    private readonly ISynologyApiClient _synologyApiClient;
    private ApiInfoData? _apiInfoData;

    public SynologyApiInfoProvider(ISynologyApiClient synologyApiClient)
    {
        _synologyApiClient = synologyApiClient;
    }
    
    public async Task<ApiInfoData> GetOrFetchInfo(CancellationToken cancellationToken)
    {
        if (_apiInfoData is not null) 
            return _apiInfoData;
        
        var apiInfoRequest = new ApiInfoRequest(
            method: SynologyApiMethods.Api.Info_Query,
            version: 1);
        var apiInfoResponse = await _synologyApiClient.ApiInfoApi.QueryAsync(apiInfoRequest, cancellationToken);
            
        _apiInfoData = apiInfoResponse.Data;

        return _apiInfoData;
    }
}