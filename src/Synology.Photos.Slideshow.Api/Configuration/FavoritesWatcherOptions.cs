namespace Synology.Photos.Slideshow.Api.Configuration;

public record FavoritesWatcherOptions
{
    public int DebounceDelayInSeconds { get; init; } = 10;
}
