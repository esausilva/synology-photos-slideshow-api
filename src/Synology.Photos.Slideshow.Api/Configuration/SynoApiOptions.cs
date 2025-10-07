using System.ComponentModel.DataAnnotations;

namespace Synology.Photos.Slideshow.Api.Configuration;

public record SynoApiOptions
{
    public IReadOnlyList<string> FileStationSearchFolders { get; init; } = [];
    public int NumberOfPhotoDownloads { get; init; } = 10;
    [Required] public string DownloadAbsolutePath { get; init; } = "";
    [Required] public string DownloadFileName { get; init; } = "";
}