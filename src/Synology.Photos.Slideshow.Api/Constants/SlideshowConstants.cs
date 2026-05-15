namespace Synology.Photos.Slideshow.Api.Constants;

public static class SlideshowConstants
{
    public const string BaseRoute = "/slideshow";
    
    public const string GeolocationHttpClient = "GeolocationHttpClient";

    public const string FavoritesFolderName = "favorites";

    // DSM creates thumbnails in a directory named "@eaDir" when accessing the photos via 'DS File'
    public const string DsmThumbnailDir = "@eaDir";

    public const string ThumbnailPostfix = "__thumb";

    public static readonly HashSet<string> ImageExtensionsForConversion = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".tiff"
    };
}