namespace Synology.Photos.Slideshow.Api.Slideshow.Hubs;

public interface ISlideshowHub
{
    Task RefreshSlideshow();
    Task PhotoProcessingError(string errorMessage);
}
