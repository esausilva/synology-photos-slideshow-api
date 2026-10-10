namespace Synology.Photos.Slideshow.Api.Slideshow.Endpoints.Request;

public record ToggleFavoriteRequest
(
    string FileName,
    bool IsFavorite,
    string? SignalRConnectionId
);
