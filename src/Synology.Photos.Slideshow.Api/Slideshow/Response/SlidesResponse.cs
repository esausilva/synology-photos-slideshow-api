namespace Synology.Photos.Slideshow.Api.Slideshow.Response;

public record SlidesResponse(
    string RelativeUrl, 
    string DateTaken, 
    string GoogleMapsLink);
