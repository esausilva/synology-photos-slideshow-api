using Synology.Photos.Slideshow.Api.Slideshow.Messaging;

namespace Synology.Photos.Slideshow.Api.Tests.UnitTests.MessagingTests;

public class PhotoThumbnailProcessingChannelTests
{
    [Test]
    public async Task Assert_PublishAsync_Makes_Message_Available_To_Reader()
    {
        var channel = new PhotoThumbnailProcessingChannel();

        await channel.PublishAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await using var enumerator = channel.ReadAllAsync(cts.Token).GetAsyncEnumerator(cts.Token);

        var hasItem = await enumerator.MoveNextAsync();

        await Assert
            .That(hasItem)
            .IsTrue();

        await Assert
            .That(enumerator.Current)
            .IsTrue();
    }

    [Test]
    public async Task Assert_PublishAsync_Preserves_Message_Order_For_Multiple_Publishes()
    {
        var channel = new PhotoThumbnailProcessingChannel();

        await channel.PublishAsync();
        await channel.PublishAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var count = 0;

        await using var enumerator = channel.ReadAllAsync(cts.Token).GetAsyncEnumerator(cts.Token);
        while (count < 2 && await enumerator.MoveNextAsync())
        {
            count++;
        }

        await Assert
            .That(count)
            .IsEqualTo(2);
    }

    [Test]
    public async Task Assert_ReadAllAsync_Respects_CancellationToken()
    {
        var channel = new PhotoThumbnailProcessingChannel();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert
            .That(async () =>
            {
                await foreach (var _ in channel.ReadAllAsync(cts.Token))
                {
                }
            })
            .Throws<OperationCanceledException>();
    }
}
