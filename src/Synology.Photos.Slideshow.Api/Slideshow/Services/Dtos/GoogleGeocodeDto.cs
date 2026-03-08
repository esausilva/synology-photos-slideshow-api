using System.Text.Json.Serialization;

namespace Synology.Photos.Slideshow.Api.Slideshow.Services.Dtos;

public record GoogleGeocodeDto(
    [property: JsonPropertyName("results")] IList<GoogleGeocodeResultDto> Results,
    [property: JsonPropertyName("status")] string Status
);

public record GoogleGeocodeResultDto(
    [property: JsonPropertyName("address_components")] IList<GoogleAddressComponentDto> AddressComponents
);

public record GoogleAddressComponentDto(
    [property: JsonPropertyName("long_name")] string LongName,
    [property: JsonPropertyName("short_name")] string ShortName,
    [property: JsonPropertyName("types")] IList<string> Types
);
