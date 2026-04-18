using System.Net;
using NSubstitute;
using Synology.Api.Sdk.SynologyApi;
using Synology.Api.Sdk.SynologyApi.ApiInfo.Request;
using Synology.Api.Sdk.SynologyApi.ApiInfo.Response;
using Synology.Api.Sdk.SynologyApi.Shared.Request;
using Synology.Photos.Slideshow.Api.Slideshow.Providers;

namespace Synology.Photos.Slideshow.Api.Tests.UnitTests.ProvidersTests;

public class SynologyApiInfoProviderTests
{
    private static readonly ApiInfoData ExpectedData = new()
    {
        SynoApiAuth = new ApiInfoDetails { MaxVersion = 7, MinVersion = 1, Path = "auth.cgi" },
        SynoFileStationSearch = new ApiInfoDetails { MaxVersion = 2, MinVersion = 1, Path = "search.cgi" },
    };

    private static (SynologyApiInfoProvider provider, ISynologyApiService apiService, ISynologyApiRequestBuilder requestBuilder) CreateProvider()
    {
        var apiService = Substitute.For<ISynologyApiService>();
        var requestBuilder = Substitute.For<ISynologyApiRequestBuilder>();

        requestBuilder
            .BuildUrl(Arg.Any<RequestBase>())
            .Returns("https://example/api/info");

        apiService
            .GetAsync<ApiInfoResponse>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ApiInfoResponse
            {
                Success = true,
                StatusCode = HttpStatusCode.OK,
                Data = ExpectedData,
            });

        return (new SynologyApiInfoProvider(apiService, requestBuilder), apiService, requestBuilder);
    }

    [Test]
    public async Task Assert_GetOrFetchInfo_Returns_Data_From_Api_On_First_Call()
    {
        var (provider, _, _) = CreateProvider();

        var result = await provider.GetOrFetchInfo(CancellationToken.None);

        await Assert
            .That(result)
            .IsNotNull();

        await Assert
            .That(result.SynoApiAuth.MaxVersion)
            .IsEqualTo(7);

        await Assert
            .That(result.SynoFileStationSearch.MaxVersion)
            .IsEqualTo(2);
    }

    [Test]
    public async Task Assert_GetOrFetchInfo_Caches_Result_Across_Calls()
    {
        var (provider, apiService, requestBuilder) = CreateProvider();

        _ = await provider.GetOrFetchInfo(CancellationToken.None);
        _ = await provider.GetOrFetchInfo(CancellationToken.None);
        _ = await provider.GetOrFetchInfo(CancellationToken.None);

        await apiService
            .Received(1)
            .GetAsync<ApiInfoResponse>(Arg.Any<string>(), Arg.Any<CancellationToken>());

        requestBuilder
            .Received(1)
            .BuildUrl(Arg.Any<ApiInfoRequest>());
    }

    [Test]
    public async Task Assert_GetOrFetchInfo_Builds_Url_From_ApiInfoRequest()
    {
        var (provider, _, requestBuilder) = CreateProvider();

        _ = await provider.GetOrFetchInfo(CancellationToken.None);

        requestBuilder
            .Received(1)
            .BuildUrl(Arg.Any<ApiInfoRequest>());
    }
}
