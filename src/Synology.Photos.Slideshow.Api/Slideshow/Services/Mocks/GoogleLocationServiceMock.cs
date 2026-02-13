namespace Synology.Photos.Slideshow.Api.Slideshow.Services.Mocks;

public class GoogleLocationServiceMock : ILocationService
{
    public async Task<string> GetLocation(double lat, double lon, CancellationToken cancellationToken)
    {
        return await Task.Run(() => "(Mock)Nashville, TN", cancellationToken);
    }
}