namespace Synology.Photos.Slideshow.Api.Slideshow.Endpoints.Request;

public record DeletePhotosRequest
(
    IList<string> PhotoNames,
    string? SignalRConnectionId
);