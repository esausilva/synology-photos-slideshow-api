using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Synology.Photos.Slideshow.Api.Configuration;
using Synology.Photos.Slideshow.Api.Constants;

namespace Synology.Photos.Slideshow.Api.Extensions;

public static class StaticFilesExtensions
{
    extension(WebApplication app)
    {
        public void ConfigureStaticFiles()
        {
            var optionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<SynoApiOptions>>();
            var photosPath = optionsMonitor.CurrentValue.DownloadAbsolutePath;
            
            if (!Directory.Exists(photosPath))
                throw new DirectoryNotFoundException($"Directory {photosPath} does not exist");
                
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(photosPath),
                RequestPath = SlideshowConstants.BaseRoute,
                OnPrepareResponse = ctx =>
                {
                    // Cache images for 7 days
                    const int durationInSeconds = 60 * 60 * 24 * 7;
                    ctx.Context.Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.CacheControl] =
                        "public,max-age=" + durationInSeconds;
                }
            });
        }
    }
}