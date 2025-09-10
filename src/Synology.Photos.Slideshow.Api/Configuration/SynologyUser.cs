using System.ComponentModel.DataAnnotations;

namespace Synology.Photos.Slideshow.Api.Configuration;

public record SynologyUser
{
    [Required] public required string Account { get; init; }
    [Required] public required string Password { get; init; }
}