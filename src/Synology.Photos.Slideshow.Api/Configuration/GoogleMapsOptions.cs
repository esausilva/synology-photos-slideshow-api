using System.ComponentModel.DataAnnotations;

namespace Synology.Photos.Slideshow.Api.Configuration;

public record GoogleMapsOptions
{
    [Required][Length(1, 100)] public required string ApiKey { get; init; }
    public bool EnableMocks { get; init; } = true;
}
