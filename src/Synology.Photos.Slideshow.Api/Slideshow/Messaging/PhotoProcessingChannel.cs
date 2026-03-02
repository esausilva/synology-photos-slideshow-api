using System.Threading.Channels;

namespace Synology.Photos.Slideshow.Api.Slideshow.Messaging;

public sealed class PhotoProcessingChannel : IPhotoProcessingChannel
{
    private readonly Channel<bool> _channel;

    public PhotoProcessingChannel()
    {
        // UnboundedChannel allows any number of messages to be queued
        // Use BoundedChannel if you want to limit the queue size
        _channel = Channel.CreateUnbounded<bool>(new UnboundedChannelOptions
        {
            SingleWriter = false, // Multiple controllers/services can write
            SingleReader = true   // Only one background service reads
        });
    }

    public async ValueTask PublishAsync(CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(true, cancellationToken);
    }

    public IAsyncEnumerable<bool> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}