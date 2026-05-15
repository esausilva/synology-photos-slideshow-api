using static Synology.Photos.Slideshow.Api.Constants.SlideshowConstants;

namespace Synology.Photos.Slideshow.Api.Slideshow.Extensions;

public static class StringExtensions
{
    extension(string value)
    {
        public bool IsNotDsmThumbnailDir()
        {
            return !value.Contains($"{Path.DirectorySeparatorChar}{DsmThumbnailDir}{Path.DirectorySeparatorChar}") && 
                   !value.Contains($"{Path.DirectorySeparatorChar}{DsmThumbnailDir}");
        }
    }
}