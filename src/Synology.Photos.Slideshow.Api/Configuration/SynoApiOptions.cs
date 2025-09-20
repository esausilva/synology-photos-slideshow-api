namespace Synology.Photos.Slideshow.Api.Configuration;

public record SynoApiOptions
{
    public IReadOnlyList<string> FileStationSearchFolders { get; init; } = [];
    public int NumberOfPhotoDownloads { get; init; }
}