using Synology.Photos.Slideshow.Api.Slideshow.Endpoints;
using Synology.Photos.Slideshow.Api.Slideshow.Hubs;

namespace Synology.Photos.Slideshow.Api.Extensions;

public static class ConfigureEndpointsExtensions
{
    extension(WebApplication app)
    {
        public void ConfigureEndpoints()
        {
            app.MapHub<SlideshowHub>("/hubs/slideshow");
            
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
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status503ServiceUnavailable)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapGet("slides", Slides.GetAsync)
                .WithName("GetPhotoUrls")
                .Produces<IList<string>>()
                .Produces(StatusCodes.Status500InternalServerError);
            
            group.MapPost("bulk-delete", DeletePhotos.PostAsync)
                .WithName("BulkDeletePhotos")
                .ProducesValidationProblem()
                .Produces<IList<string>>()
                .Produces<IList<string>>(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError);
            
            return group;
        }
    }
}