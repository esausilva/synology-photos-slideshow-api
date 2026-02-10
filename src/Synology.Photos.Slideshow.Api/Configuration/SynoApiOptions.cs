using System.ComponentModel.DataAnnotations;

namespace Synology.Photos.Slideshow.Api.Configuration;

public record SynoApiOptions
{
    public IReadOnlyList<string> FileStationSearchFolders { get; init; } = [];
    public int NumberOfPhotoDownloads { get; init; } = 10;
    public int ApiSearchTimeoutInSeconds { get; init; } = 60;
    [Required] [MinLength(1)] public required string DownloadAbsolutePath { get; init; }
    [Required] [MinLength(1)] public required string DownloadFileName { get; init; }
}