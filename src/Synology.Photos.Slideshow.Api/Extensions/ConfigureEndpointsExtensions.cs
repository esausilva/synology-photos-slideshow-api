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
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status503ServiceUnavailable)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapGet("slides", Slides.GetAsync)
                .WithName("GetPhotoUrls")
                .Produces<IList<string>>()
                .Produces(StatusCodes.Status500InternalServerError);
            
            group.MapPost("bulk-delete", DeletePhoto.PostAsync)
                .WithName("BulkDeletePhotos")
                .Produces<IList<string>>()
                .Produces<IList<string>>(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError);
            
            return group;
        }
    }
}