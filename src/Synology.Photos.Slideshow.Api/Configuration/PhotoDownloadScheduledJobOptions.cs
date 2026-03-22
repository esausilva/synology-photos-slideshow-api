using System.ComponentModel.DataAnnotations;

namespace Synology.Photos.Slideshow.Api.Configuration;

public record PhotoDownloadScheduledJobOptions
{
    public bool Enabled { get; init; } = true;
    
    [Required]
    [Range(0, 6)]
    public int DayOfWeek { get; init; }
    
    [Required]
    [Range(0, 23)]
    public int Hour { get; init; }
    
    [Required]
    [Range(0, 59)]
    public int Minute { get; init; }
    
    [Required]
    [MinLength(1)]
    public required string TimeZoneId { get; init; }
}
