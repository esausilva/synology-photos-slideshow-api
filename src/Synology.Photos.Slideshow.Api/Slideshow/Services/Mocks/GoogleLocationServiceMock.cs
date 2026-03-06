namespace Synology.Photos.Slideshow.Api.Slideshow.Services.Mocks;

public class GoogleLocationServiceMock : ILocationService
{
    public Task<string> GetLocation(double lat, double lon, CancellationToken cancellationToken)
    {
        return Task.FromResult("(Mock)Nashville, TN");
    }
}