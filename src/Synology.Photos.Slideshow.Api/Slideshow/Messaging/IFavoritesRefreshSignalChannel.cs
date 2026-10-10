namespace Synology.Photos.Slideshow.Api.Slideshow.Messaging;

public interface IFavoritesRefreshSignalChannel
{
    ValueTask PublishAsync(string? signalRConnectionId, CancellationToken cancellationToken = default);
    ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default);
    bool TryRead(out string? signalRConnectionId);
}
