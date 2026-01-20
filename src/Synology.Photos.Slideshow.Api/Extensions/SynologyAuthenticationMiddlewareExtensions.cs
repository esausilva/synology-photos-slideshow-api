using Synology.Photos.Slideshow.Api.Middleware;

namespace Synology.Photos.Slideshow.Api.Extensions;

public static class SynologyAuthenticationMiddlewareExtensions
{
    extension(IApplicationBuilder builder)
    {
        public IApplicationBuilder UseSynologyAuthentication()
        {
            return builder.UseMiddleware<SynologyAuthenticationMiddleware>();
        }
    }
}
