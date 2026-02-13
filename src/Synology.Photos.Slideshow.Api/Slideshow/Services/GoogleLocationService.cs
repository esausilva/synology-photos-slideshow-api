using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using Synology.Photos.Slideshow.Api.Configuration;
using Synology.Photos.Slideshow.Api.Constants;
using Synology.Photos.Slideshow.Api.Slideshow.Response;

namespace Synology.Photos.Slideshow.Api.Slideshow.Services;

public partial class GoogleLocationService : ILocationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GoogleLocationService> _logger;
    private readonly HybridCache _cache;
    private readonly string _apiKey;

    public GoogleLocationService(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<GoogleMapsOptions> googleMapsOptions,
        HybridCache cache,
        ILogger<GoogleLocationService> logger)
    {
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _apiKey = googleMapsOptions.CurrentValue.ApiKey;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the location details, such as city and state, for the specified latitude and longitude.
    /// Results are cached to reduce redundant external API calls.
    /// </summary>
    /// <param name="lat">The latitude coordinate of the location.</param>
    /// <param name="lon">The longitude coordinate of the location.</param>
    /// <param name="cancellationToken">A CancellationToken to observe while waiting for the task to complete.</param>
    /// <returns>A string representing the location (e.g., city and state) if successful, otherwise
    /// an empty string.</returns>
    public async Task<string> GetLocation(double lat, double lon, CancellationToken cancellationToken)
    {
        const string locationCacheTag = "Location";
        
        var cacheKey = $"{lat}{lon}";

        LogCheckingCache(cacheKey);

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async cancel =>
            {
                LogCacheMiss(cacheKey);
                return await GetLocationFromSource(lat, lon, cancel);
            },
            tags: [locationCacheTag],
            cancellationToken: cancellationToken
        );
    }

    private async Task<string> GetLocationFromSource(double lat, double lon, CancellationToken cancellationToken)
    {
        LogGettingLocationForLatLatLonLon(lat, lon);

        var geocodeUrl = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={lat},{lon}&key={_apiKey}";
        var httpClient = _httpClientFactory.CreateClient(SlideshowConstants.GeolocationHttpClient);

        try
        {
            var response = await httpClient.GetFromJsonAsync<GoogleGeocodeResponse>(geocodeUrl, cancellationToken);

            if (response is not { Status: "OK", Results.Count: > 0 })
                return string.Empty;

            // Google returns multiple results (street level, neighborhood level, etc.)
            // We usually want the most specific one (the first result)
            var components = response.Results[0].AddressComponents;

            const string typeCity = "locality";
            const string typeState = "administrative_area_level_1";

            var city = components
                .FirstOrDefault(c => c.Types.Contains(typeCity))?.LongName;

            var state = components
                .FirstOrDefault(c => c.Types.Contains(typeState))?.ShortName;

            if (city is not null && state is not null)
                return $"{city}, {state}";

            _logger.LogWarning("Failed to get city and state from Google Maps API");

            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get location from Google Maps API");
            return string.Empty;
        }
    }

    [LoggerMessage(LogLevel.Debug, "Checking cache for location with key: '{cacheKey}'")]
    partial void LogCheckingCache(string cacheKey);

    [LoggerMessage(LogLevel.Information, "Cache miss for key: '{cacheKey}', fetching from source")]
    partial void LogCacheMiss(string cacheKey);

    [LoggerMessage(LogLevel.Information, "Getting location for lat: '{lat}', lon: '{lon}' from Google Maps API")]
    partial void LogGettingLocationForLatLatLonLon(double lat, double lon);
}