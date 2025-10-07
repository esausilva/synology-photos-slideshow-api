using Synology.Photos.Slideshow.Api.Slideshow.DownloadPhotos.Endpoints;
using Synology.Photos.Slideshow.Api.Slideshow.Web.Endpoints;

namespace Synology.Photos.Slideshow.Api.Extensions;

public static class ConfigureEndpointsExtensions
{
    public static void ConfigureEndpoints(this WebApplication app)
    {
        app.MapGet("download-photos", DownloadPhotos.GetAsync)
            .WithName("DownloadPhotos")
            .Produces<IList<string>>();

        app.MapGet("get-photo-urls", Slides.GetAsync)
            .WithName("GetPhotoUrls")
            .Produces<IList<string>>();
    }
}