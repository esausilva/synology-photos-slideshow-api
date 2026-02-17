namespace Synology.Photos.Slideshow.Api.Configuration;

public record ThirdPartyServices
{
    public bool EnableGeolocation { get; init; } = false;
    public bool EnableDistributedCache { get; init; } = false;
}
