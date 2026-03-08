namespace Synology.Photos.Slideshow.Api.Slideshow.Endpoints.Response;

public record SlideResponse(
    string RelativeUrl, 
    string DateTaken, 
    string GoogleMapsLink,
    string Location);
