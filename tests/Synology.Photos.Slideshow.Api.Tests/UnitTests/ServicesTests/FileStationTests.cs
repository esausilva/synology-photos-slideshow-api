using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Synology.Api.Sdk.SynologyApi;
using Synology.Api.Sdk.SynologyApi.ApiInfo.Response;
using Synology.Api.Sdk.SynologyApi.FileStation.Request;
using Synology.Api.Sdk.SynologyApi.FileStation.Response;
using Synology.Api.Sdk.SynologyApi.Shared.Request;
using Synology.Api.Sdk.SynologyApi.Shared.Response;
using Synology.Photos.Slideshow.Api.Configuration;
using Synology.Photos.Slideshow.Api.Slideshow.Auth;
using Synology.Photos.Slideshow.Api.Slideshow.Providers;
using Synology.Photos.Slideshow.Api.Slideshow.Services;
using Synology.Photos.Slideshow.Api.Tests.Extensions;
using FileStationService = Synology.Photos.Slideshow.Api.Slideshow.Services.FileStation;

namespace Synology.Photos.Slideshow.Api.Tests.UnitTests.ServicesTests;

public class FileStationTests
{
    private const string SynoToken = "syno-token";

    private sealed record TestHarness(
        FileStationService Service,
        ISynologyApiService ApiService,
        ISynologyApiRequestBuilder RequestBuilder,
        ISynologyAuthenticationContext AuthContext,
        IFileProcessor FileProcessor);

    private static TestHarness CreateHarness(int downloadMaxVersion = 2)
    {
        var apiInfoProvider = Substitute.For<ISynologyApiInfoProvider>();
        var apiService = Substitute.For<ISynologyApiService>();
        var requestBuilder = Substitute.For<ISynologyApiRequestBuilder>();
        var authContext = Substitute.For<ISynologyAuthenticationContext>();
        var fileProcessor = Substitute.For<IFileProcessor>();

        authContext.GetSynoToken().Returns(SynoToken);

        apiInfoProvider.GetOrFetchInfo(Arg.Any<CancellationToken>())
            .Returns(new ApiInfoData
            {
                SynoFileStationDownload = new ApiInfoDetails { MaxVersion = downloadMaxVersion, MinVersion = 1 },
            });

        requestBuilder.BuildUrl(Arg.Any<RequestBase>()).Returns("https://example/url");

        // HttpResponse == null causes DownloadHelpers.DownloadImageOrZipFromFileStationApi to return early
        // so the filesystem side effect never runs, keeping this a pure unit test.
        apiService.GetRawResponseAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new RawResponse
            {
                Success = true,
                StatusCode = HttpStatusCode.OK,
                HttpResponse = null,
            });

        var options = new OptionsMonitorStub<SynoApiOptions>(new SynoApiOptions
        {
            DownloadAbsolutePath = "/tmp",
            DownloadFileName = "download.zip",
        });

        var service = new FileStationService(
            apiInfoProvider,
            apiService,
            requestBuilder,
            authContext,
            options,
            fileProcessor,
            NullLogger<FileStationService>.Instance);

        return new TestHarness(service, apiService, requestBuilder, authContext, fileProcessor);
    }

    [Test]
    public async Task Assert_Download_Cleans_Directory_Before_Requesting_Photos()
    {
        var harness = CreateHarness();
        var items = new List<FileStationItem>
        {
            new() { Name = "one.jpg", Path = "/photos/one.jpg" },
        };

        await harness.Service.Download(items, SynoToken, CancellationToken.None);

        await harness.FileProcessor
            .Received(1)
            .CleanDownloadDirectory(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Assert_Download_Builds_Url_From_FileStationDownloadRequest()
    {
        var harness = CreateHarness();
        var items = new List<FileStationItem>
        {
            new() { Name = "one.jpg", Path = "/photos/one.jpg" },
        };

        await harness.Service.Download(items, SynoToken, CancellationToken.None);

        harness.RequestBuilder
            .Received(1)
            .BuildUrl(Arg.Any<FileStationDownloadRequest>());
    }

    [Test]
    public async Task Assert_Download_Invokes_ApiService_For_Raw_Response()
    {
        var harness = CreateHarness();
        var items = new List<FileStationItem>
        {
            new() { Name = "one.jpg", Path = "/photos/one.jpg" },
        };

        await harness.Service.Download(items, SynoToken, CancellationToken.None);

        await harness.ApiService
            .Received(1)
            .GetRawResponseAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Assert_Download_Without_SynoToken_Resolves_Token_From_AuthContext()
    {
        var harness = CreateHarness();
        var items = new List<FileStationItem>
        {
            new() { Name = "one.jpg", Path = "/photos/one.jpg" },
        };

        await harness.Service.Download(items, CancellationToken.None);

        harness.AuthContext
            .Received(1)
            .GetSynoToken();
    }
}
