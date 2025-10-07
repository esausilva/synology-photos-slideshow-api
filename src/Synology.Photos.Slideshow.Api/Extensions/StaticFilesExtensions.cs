using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Synology.Photos.Slideshow.Api.Configuration;
using Synology.Photos.Slideshow.Api.Constants;

namespace Synology.Photos.Slideshow.Api.Extensions;

public static class StaticFilesExtensions
{
    public static void ConfigureStaticFiles(this WebApplication app)
    {
        var optionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<SynoApiOptions>>();
        var photosPath = optionsMonitor.CurrentValue.DownloadAbsolutePath;
        
        if (!Directory.Exists(photosPath))
            throw new DirectoryNotFoundException($"Directory {photosPath} does not exist");
            
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(photosPath),
            RequestPath = SlideshowConstants.BaseRoute
        });
    }
}