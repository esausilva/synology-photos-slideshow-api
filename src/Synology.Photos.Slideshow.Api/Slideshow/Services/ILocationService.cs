namespace Synology.Photos.Slideshow.Api.Slideshow.Services;

public interface ILocationService
{
    public Task<string> GetLocation(double lat, double lon);
}