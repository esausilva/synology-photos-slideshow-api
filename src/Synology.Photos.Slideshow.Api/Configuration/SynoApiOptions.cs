using System.ComponentModel.DataAnnotations;

namespace Synology.Photos.Slideshow.Api.Configuration;

public record SynoApiOptions
{
    public IReadOnlyList<string> FileStationSearchFolders { get; init; } = [];
    public int NumberOfPhotoDownloads { get; init; } = 10;
    [Required] public required string DownloadAbsolutePath { get; init; }
    [Required] public required string DownloadFileName { get; init; }
}