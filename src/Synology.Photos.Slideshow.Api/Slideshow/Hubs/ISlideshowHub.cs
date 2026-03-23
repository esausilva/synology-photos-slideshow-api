namespace Synology.Photos.Slideshow.Api.Slideshow.Hubs;

public interface ISlideshowHub
{
    Task RefreshSlideshow();
    Task PhotoProcessingError(string errorMessage);
    Task RefreshGallery();
    Task ThumbnailsProcessingError(string errorMessage);
}
