using Synology.Api.Sdk.Constants;
using Synology.Api.Sdk.SynologyApi;
using Synology.Api.Sdk.SynologyApi.ApiInfo.Request;
using Synology.Api.Sdk.SynologyApi.ApiInfo.Response;

namespace Synology.Photos.Slideshow.Api.Slideshow.Common;

public sealed class SynologyApiInfo : ISynologyApiInfo
{
    private readonly ISynologyApiService _synoApiService;
    private readonly ISynologyApiRequestBuilder _synoApiRequestBuilder;
    private ApiInfoData? _apiInfoData;

    public SynologyApiInfo(ISynologyApiService synoApiService, ISynologyApiRequestBuilder synoApiRequestBuilder)
    {
        _synoApiService = synoApiService;
        _synoApiRequestBuilder = synoApiRequestBuilder;
    }
    
    public async Task<ApiInfoData> GetApiInfo(CancellationToken cancellationToken)
    {
        if (_apiInfoData is not null) 
            return _apiInfoData;
        
        var apiInfoRequest = new ApiInfoRequest(
            method: SynologyApiMethods.Api.Info_Query,
            version: 1);
        var apiInfoUrl = _synoApiRequestBuilder.BuildUrl(apiInfoRequest);
        var apiInfoResponse = await _synoApiService.GetAsync<ApiInfoResponse>(apiInfoUrl, cancellationToken);
            
        _apiInfoData = apiInfoResponse.Data;

        return _apiInfoData;
    }
}