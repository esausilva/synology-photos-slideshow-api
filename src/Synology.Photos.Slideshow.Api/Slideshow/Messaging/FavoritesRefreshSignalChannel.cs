using System.Threading.Channels;

namespace Synology.Photos.Slideshow.Api.Slideshow.Messaging;

public sealed class FavoritesRefreshSignalChannel : IFavoritesRefreshSignalChannel
{
    private readonly Channel<string?> _channel;

    public FavoritesRefreshSignalChannel()
    {
        _channel = Channel.CreateUnbounded<string?>(new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = true
        });
    }

    public async ValueTask PublishAsync(string? signalRConnectionId, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(signalRConnectionId, cancellationToken);
    }

    public ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.WaitToReadAsync(cancellationToken);
    }

    public bool TryRead(out string? signalRConnectionId)
    {
        return _channel.Reader.TryRead(out signalRConnectionId);
    }
}
