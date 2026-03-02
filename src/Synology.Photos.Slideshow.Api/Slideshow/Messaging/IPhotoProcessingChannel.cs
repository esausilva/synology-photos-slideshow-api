namespace Synology.Photos.Slideshow.Api.Slideshow.Messaging;

public interface IPhotoProcessingChannel
{
    ValueTask PublishAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<bool> ReadAllAsync(CancellationToken cancellationToken = default);
}