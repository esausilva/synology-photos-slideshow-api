using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Synology.Api.Sdk.SynologyApi;
using Synology.Api.Sdk.SynologyApi.ApiInfo.Response;
using Synology.Api.Sdk.SynologyApi.FileStation.Response;
using Synology.Api.Sdk.SynologyApi.Shared.Request;
using Synology.Photos.Slideshow.Api.Configuration;
using Synology.Photos.Slideshow.Api.Slideshow.Auth;
using Synology.Photos.Slideshow.Api.Slideshow.Providers;
using Synology.Photos.Slideshow.Api.Slideshow.Services;
using Synology.Photos.Slideshow.Api.Tests.Extensions;

namespace Synology.Photos.Slideshow.Api.Tests.UnitTests.ServicesTests;

public class NasPhotoSearchServiceTests
{
    private const string SynoToken = "syno-token";

    private sealed record TestHarness(
        NasPhotoSearchService Service,
        ISynologyApiInfoProvider ApiInfoProvider,
        ISynologyApiService ApiService,
        ISynologyApiRequestBuilder RequestBuilder,
        ISynologyAuthenticationContext AuthContext);

    private static TestHarness CreateHarness(int photoDownloadCount = 1, int maxVersion = 2)
    {
        var apiInfoProvider = Substitute.For<ISynologyApiInfoProvider>();
        var apiService = Substitute.For<ISynologyApiService>();
        var requestBuilder = Substitute.For<ISynologyApiRequestBuilder>();
        var authContext = Substitute.For<ISynologyAuthenticationContext>();

        authContext.GetSynoToken().Returns(SynoToken);

        apiInfoProvider.GetOrFetchInfo(Arg.Any<CancellationToken>())
            .Returns(new ApiInfoData
            {
                SynoFileStationSearch = new ApiInfoDetails { MaxVersion = maxVersion, MinVersion = 1 },
            });

        requestBuilder.BuildUrl(Arg.Any<RequestBase>()).Returns("https://example/url");

        var options = new OptionsMonitorStub<SynoApiOptions>(new SynoApiOptions
        {
            DownloadAbsolutePath = "/tmp",
            DownloadFileName = "download.zip",
            FileStationSearchFolders = ["/photos"],
            NumberOfPhotoDownloads = photoDownloadCount,
            ApiSearchTimeoutInSeconds = 60,
        });

        var service = new NasPhotoSearchService(
            apiInfoProvider,
            apiService,
            requestBuilder,
            authContext,
            options,
            NullLogger<NasPhotoSearchService>.Instance);

        return new TestHarness(service, apiInfoProvider, apiService, requestBuilder, authContext);
    }

    private static FileStationSearchStartResponse StartResponse(string taskId)
        => new()
        {
            Success = true,
            StatusCode = HttpStatusCode.OK,
            Data = new FileStationSearchStartData { TaskId = taskId },
        };

    private static FileStationSearchListResponse ListResponse(
        int total,
        bool finished,
        params FileStationItem[] files)
        => new()
        {
            Success = true,
            StatusCode = HttpStatusCode.OK,
            Data = new FileStationSearchListData
            {
                Total = total,
                Finished = finished,
                Files = files,
            },
        };

    [Test]
    public async Task Assert_SearchPhotos_Returns_Failed_When_Api_Version_Is_Zero()
    {
        var harness = CreateHarness(maxVersion: 0);

        var result = await harness.Service.SearchPhotos(SynoToken, CancellationToken.None);

        await Assert
            .That(result.IsFailed)
            .IsTrue();

        await harness.ApiService
            .DidNotReceive()
            .GetAsync<FileStationSearchStartResponse>(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Assert_SearchPhotos_Returns_Failed_When_Start_Returns_Empty_TaskId()
    {
        var harness = CreateHarness();

        harness.ApiService
            .GetAsync<FileStationSearchStartResponse>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(StartResponse(taskId: string.Empty));

        var result = await harness.Service.SearchPhotos(SynoToken, CancellationToken.None);

        await Assert
            .That(result.IsFailed)
            .IsTrue();
    }

    [Test]
    public async Task Assert_SearchPhotos_Returns_Success_With_NonVideo_Files()
    {
        var harness = CreateHarness(photoDownloadCount: 2);

        harness.ApiService
            .GetAsync<FileStationSearchStartResponse>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(StartResponse("task-1"));

        harness.ApiService
            .GetAsync<FileStationSearchListResponse>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(
                ListResponse(total: 2, finished: true),
                ListResponse(total: 2, finished: true,
                    new FileStationItem { Name = "one.jpg", Path = "/photos/one.jpg" }),
                ListResponse(total: 2, finished: true,
                    new FileStationItem { Name = "two.jpg", Path = "/photos/two.jpg" }));

        var result = await harness.Service.SearchPhotos(SynoToken, CancellationToken.None);

        await Assert
            .That(result.IsSuccess)
            .IsTrue();

        await Assert
            .That(result.Value.Count)
            .IsEqualTo(2);

        await Assert
            .That(result.Value.All(i => i.Path.EndsWith(".jpg")))
            .IsTrue();
    }

    [Test]
    public async Task Assert_SearchPhotos_Skips_Video_Files()
    {
        var harness = CreateHarness(photoDownloadCount: 1);

        harness.ApiService
            .GetAsync<FileStationSearchStartResponse>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(StartResponse("task-video"));

        harness.ApiService
            .GetAsync<FileStationSearchListResponse>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(
                ListResponse(total: 5, finished: true),
                ListResponse(total: 5, finished: true,
                    new FileStationItem { Name = "clip.mp4", Path = "/photos/clip.mp4" }),
                ListResponse(total: 5, finished: true,
                    new FileStationItem { Name = "pic.jpg", Path = "/photos/pic.jpg" }));

        var result = await harness.Service.SearchPhotos(SynoToken, CancellationToken.None);

        await Assert
            .That(result.IsSuccess)
            .IsTrue();

        await Assert
            .That(result.Value.Count)
            .IsEqualTo(1);

        await Assert
            .That(result.Value[0].Path)
            .IsEqualTo("/photos/pic.jpg");
    }

    [Test]
    public async Task Assert_SearchPhotos_Returns_Empty_When_Total_Photo_Count_Is_Zero()
    {
        var harness = CreateHarness();

        harness.ApiService
            .GetAsync<FileStationSearchStartResponse>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(StartResponse("task-empty"));

        harness.ApiService
            .GetAsync<FileStationSearchListResponse>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ListResponse(total: 0, finished: true));

        var result = await harness.Service.SearchPhotos(SynoToken, CancellationToken.None);

        await Assert
            .That(result.IsSuccess)
            .IsTrue();

        await Assert
            .That(result.Value.Count)
            .IsEqualTo(0);
    }

    [Test]
    public async Task Assert_SearchPhotos_Uses_Token_From_AuthContext_When_Overload_Has_No_Token()
    {
        var harness = CreateHarness();

        harness.ApiService
            .GetAsync<FileStationSearchStartResponse>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(StartResponse("task-auth"));

        harness.ApiService
            .GetAsync<FileStationSearchListResponse>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(
                ListResponse(total: 1, finished: true),
                ListResponse(total: 1, finished: true,
                    new FileStationItem { Name = "p.jpg", Path = "/photos/p.jpg" }));

        _ = await harness.Service.SearchPhotos(CancellationToken.None);

        harness.AuthContext
            .Received(1)
            .GetSynoToken();
    }
}
