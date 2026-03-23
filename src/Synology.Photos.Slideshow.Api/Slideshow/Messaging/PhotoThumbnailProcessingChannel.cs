using System.Threading.Channels;

namespace Synology.Photos.Slideshow.Api.Slideshow.Messaging;

public sealed class PhotoThumbnailProcessingChannel : IPhotoThumbnailProcessingChannel
{
    private readonly Channel<bool> _channel;

    public PhotoThumbnailProcessingChannel()
    {
        _channel = Channel.CreateUnbounded<bool>(new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = true
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
