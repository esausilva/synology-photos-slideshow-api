using System.Text.Json.Serialization;

namespace Synology.Photos.Slideshow.Api.Slideshow.Response;

public record GoogleGeocodeResponse(
    [property: JsonPropertyName("results")] IList<GoogleGeocodeResult> Results,
    [property: JsonPropertyName("status")] string Status
);

public record GoogleGeocodeResult(
    [property: JsonPropertyName("address_components")] IList<GoogleAddressComponent> AddressComponents
);

public record GoogleAddressComponent(
    [property: JsonPropertyName("long_name")] string LongName,
    [property: JsonPropertyName("short_name")] string ShortName,
    [property: JsonPropertyName("types")] IList<string> Types
);
