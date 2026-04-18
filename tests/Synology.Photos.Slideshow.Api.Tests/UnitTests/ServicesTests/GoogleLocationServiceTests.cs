using System.Net;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Synology.Photos.Slideshow.Api.Configuration;
using Synology.Photos.Slideshow.Api.Slideshow.Services;
using Synology.Photos.Slideshow.Api.Tests.Extensions;

namespace Synology.Photos.Slideshow.Api.Tests.UnitTests.ServicesTests;

public class GoogleLocationServiceTests
{
    private sealed record TestHarness(
        GoogleLocationService Service,
        IHttpClientFactory HttpClientFactory,
        HttpMessageHandler HttpMessageHandler);

    private static TestHarness CreateHarness(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = Substitute.ForPartsOf<HttpMessageHandler>();
        handler
            .SendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json"),
            }));

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(handler));

        var services = new ServiceCollection();
        services.AddHybridCache();
        var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();

        var options = new OptionsMonitorStub<GoogleMapsOptions>(new GoogleMapsOptions
        {
            ApiKey = "test-api-key",
            EnableMocks = false,
        });

        var service = new GoogleLocationService(
            httpClientFactory,
            options,
            cache,
            NullLogger<GoogleLocationService>.Instance);

        return new TestHarness(service, httpClientFactory, handler);
    }

    private const string OkResponse = """
        {
          "status": "OK",
          "results": [
            {
              "address_components": [
                { "long_name": "Nashville", "short_name": "Nashville", "types": ["locality", "political"] },
                { "long_name": "Tennessee", "short_name": "TN", "types": ["administrative_area_level_1", "political"] }
              ]
            }
          ]
        }
        """;

    private const string ZeroResultsResponse = """{ "status": "ZERO_RESULTS", "results": [] }""";

    private const string NoMatchingComponentsResponse = """
        {
          "status": "OK",
          "results": [
            {
              "address_components": [
                { "long_name": "X", "short_name": "X", "types": ["postal_code"] }
              ]
            }
          ]
        }
        """;

    [Test]
    public async Task Assert_GetLocation_Returns_City_And_State_On_Successful_Response()
    {
        var harness = CreateHarness(OkResponse);

        var location = await harness.Service.GetLocation(36.16, -86.78, CancellationToken.None);

        await Assert
            .That(location)
            .IsEqualTo("Nashville, TN");
    }

    [Test]
    public async Task Assert_GetLocation_Returns_Empty_When_Status_Is_Not_OK()
    {
        var harness = CreateHarness(ZeroResultsResponse);

        var location = await harness.Service.GetLocation(0.0, 0.0, CancellationToken.None);

        await Assert
            .That(location)
            .IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Assert_GetLocation_Returns_Empty_When_City_Or_State_Is_Missing()
    {
        var harness = CreateHarness(NoMatchingComponentsResponse);

        var location = await harness.Service.GetLocation(10.0, 20.0, CancellationToken.None);

        await Assert
            .That(location)
            .IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Assert_GetLocation_Caches_Results_For_Same_Coordinates()
    {
        var harness = CreateHarness(OkResponse);

        var first = await harness.Service.GetLocation(1.5, -2.5, CancellationToken.None);
        var second = await harness.Service.GetLocation(1.5, -2.5, CancellationToken.None);
        var third = await harness.Service.GetLocation(1.5, -2.5, CancellationToken.None);

        await Assert
            .That(first)
            .IsEqualTo("Nashville, TN");

        await Assert
            .That(second)
            .IsEqualTo(first);

        await Assert
            .That(third)
            .IsEqualTo(first);

        await harness.HttpMessageHandler
            .Received(1)
            .SendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Assert_GetLocation_Returns_Empty_On_Http_Exception()
    {
        var handler = Substitute.ForPartsOf<HttpMessageHandler>();
        handler
            .SendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns<Task<HttpResponseMessage>>(_ => throw new HttpRequestException("network down"));

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(handler));

        var services = new ServiceCollection();
        services.AddHybridCache();
        var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();

        var options = new OptionsMonitorStub<GoogleMapsOptions>(new GoogleMapsOptions
        {
            ApiKey = "test-api-key",
            EnableMocks = false,
        });

        var service = new GoogleLocationService(
            httpClientFactory,
            options,
            cache,
            NullLogger<GoogleLocationService>.Instance);

        var location = await service.GetLocation(99.0, 99.0, CancellationToken.None);

        await Assert
            .That(location)
            .IsEqualTo(string.Empty);
    }
}
