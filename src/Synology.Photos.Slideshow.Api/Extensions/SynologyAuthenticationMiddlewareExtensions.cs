using Synology.Photos.Slideshow.Api.Middleware;

namespace Synology.Photos.Slideshow.Api.Extensions;

public static class SynologyAuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseSynologyAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SynologyAuthenticationMiddleware>();
    }
}
