using Microsoft.Extensions.Options;
using Synology.Photos.Slideshow.Api.Configuration;
using Synology.Photos.Slideshow.Api.Constants;
using Synology.Photos.Slideshow.Api.Slideshow.Response;

namespace Synology.Photos.Slideshow.Api.Slideshow.Services;

public partial class GoogleLocationService : ILocationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GoogleLocationService> _logger;
    private readonly string _apiKey;

    public GoogleLocationService(
        IHttpClientFactory httpClientFactory, 
        IOptionsMonitor<GoogleMapsOptions> googleMapsOptions,
        ILogger<GoogleLocationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _apiKey = googleMapsOptions.CurrentValue.ApiKey;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the location details, such as city and state, for the specified latitude and longitude.
    /// </summary>
    /// <param name="lat">The latitude coordinate of the location.</param>
    /// <param name="lon">The longitude coordinate of the location.</param>
    /// <returns>A string representing the location (e.g., city and state) if successful, otherwise
    /// an empty string.</returns>
    public async Task<string> GetLocation(double lat, double lon)
    {
        // TODO: Read (or Update) from Cache

        LogGettingLocationForLatLatLonLon(lat, lon);
        
        var geocodeUrl = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={lat},{lon}&key={_apiKey}";
        var httpClient = _httpClientFactory.CreateClient(SlideshowConstants.GeolocationHttpClient);
        
        try
        {
            var response = await httpClient.GetFromJsonAsync<GoogleGeocodeResponse>(geocodeUrl);

            if (response is not { Status: "OK", Results.Count: > 0 })
            {
                return string.Empty;
            }

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
            {
                return $"{city}, {state}";
            }
            
            _logger.LogWarning("Failed to get city and state from Google Maps API");

            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get location from Google Maps API");
            return string.Empty;
        }
    }

    [LoggerMessage(LogLevel.Information, "Getting location for lat: {lat}, lon: {lon}")]
    partial void LogGettingLocationForLatLatLonLon(double lat, double lon);
}