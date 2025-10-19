using Synology.Photos.Slideshow.Api.Configuration;
using Synology.Photos.Slideshow.Api.Slideshow.Auth;
using Synology.Photos.Slideshow.Api.Slideshow.Common;
using Synology.Photos.Slideshow.Api.Slideshow.DownloadPhotos.Services;
using Synology.Photos.Slideshow.Api.Slideshow.Web.Services;

namespace Synology.Photos.Slideshow.Api.Extensions;

public static class ConfigurationExtensions
{
    public static IServiceCollection ConfigureServices(
        this IServiceCollection services, 
        ConfigurationManager configuration
    )
    {
        services
            .AddOptionsWithValidateOnStart<SynologyUser>()
            .ValidateDataAnnotations()
            .Bind(configuration.GetSection(nameof(SynologyUser)));
        
        services
            .AddOptionsWithValidateOnStart<SynoApiOptions>()
            .ValidateDataAnnotations()
            .Bind(configuration.GetSection(nameof(SynoApiOptions)));

        services.AddHttpContextAccessor();
        
        services.AddScoped<ISynologyAuthenticationContext, SynologyAuthenticationContext>();
        services.AddSingleton<ISynologyApiInfo, SynologyApiInfo>();
        services.AddTransient<ISynologyApiSearch, SynologyApiSearch>();
        services.AddTransient<IFileStation, FileStation>();
        services.AddTransient<IFileProcessing, FileProcessing>();
        services.AddTransient<IPhotosService, PhotosService>();
  
        return services;
    }
}