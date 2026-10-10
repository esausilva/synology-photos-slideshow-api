using Synology.Photos.Slideshow.Api.Slideshow.Messaging;

namespace Synology.Photos.Slideshow.Api.Tests.UnitTests.MessagingTests;

public class FavoritesRefreshSignalChannelTests
{
    [Test]
    public async Task Assert_PublishAsync_Makes_Connection_Ids_Available_In_Order()
    {
        var channel = new FavoritesRefreshSignalChannel();

        await channel.PublishAsync("connection-1");
        await channel.PublishAsync(null);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var hasItem = await channel.WaitToReadAsync(cts.Token);

        await Assert.That(hasItem).IsTrue();
        await Assert.That(channel.TryRead(out var first)).IsTrue();
        await Assert.That(first).IsEqualTo("connection-1");
        await Assert.That(channel.TryRead(out var second)).IsTrue();
        await Assert.That(second).IsNull();
        await Assert.That(channel.TryRead(out _)).IsFalse();
    }
}
