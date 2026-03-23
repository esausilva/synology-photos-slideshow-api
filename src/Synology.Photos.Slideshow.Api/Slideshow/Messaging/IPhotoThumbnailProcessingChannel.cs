namespace Synology.Photos.Slideshow.Api.Slideshow.Messaging;

public interface IPhotoThumbnailProcessingChannel
{
    ValueTask PublishAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<bool> ReadAllAsync(CancellationToken cancellationToken = default);
}
