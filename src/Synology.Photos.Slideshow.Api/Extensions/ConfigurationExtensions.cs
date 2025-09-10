using Synology.Photos.Slideshow.Api.Configuration;
using Synology.Photos.Slideshow.Api.Slideshow.Auth;
using Synology.Photos.Slideshow.Api.Slideshow.Common;

namespace Synology.Photos.Slideshow.Api.Extensions;

public static class ConfigurationExtensions
{
    public static IServiceCollection ConfigureServices(
        this IServiceCollection services, 
        ConfigurationManager configuration
    )
    {
        services
            .AddOptions<SynologyUser>()
            .Bind(configuration.GetSection(nameof(SynologyUser)))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpContextAccessor();
        services.AddScoped<ISynologyAuthenticationContext, SynologyAuthenticationContext>();
        services.AddSingleton<ISynologyApiInfo, SynologyApiInfo>();
  
        return services;
    }
}