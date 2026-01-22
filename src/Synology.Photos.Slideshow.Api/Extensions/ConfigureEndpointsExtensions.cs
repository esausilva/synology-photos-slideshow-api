using Synology.Photos.Slideshow.Api.Slideshow.Endpoints;

namespace Synology.Photos.Slideshow.Api.Extensions;

public static class ConfigureEndpointsExtensions
{
    extension(WebApplication app)
    {
        public void ConfigureEndpoints()
        {
            app.MapGroup("photos")
                .WithPhotosPrefix();
        }
    }

    extension(RouteGroupBuilder group)
    {
        private RouteGroupBuilder WithPhotosPrefix()
        {
            group.MapGet("download", DownloadPhotos.GetAsync)
                .WithName("DownloadPhotos")
                .Produces<IList<string>>();

            group.MapGet("relative-urls", Slides.GetAsync)
                .WithName("GetPhotoUrls")
                .Produces<IList<string>>();
            
            group.MapPost("bulk-delete", DeletePhoto.PostAsync)
                .WithName("BulkDeletePhotos")
                .Produces(StatusCodes.Status204NoContent)
                .Produces<IList<string>>(StatusCodes.Status404NotFound);
            
            return group;
        }
    }
}