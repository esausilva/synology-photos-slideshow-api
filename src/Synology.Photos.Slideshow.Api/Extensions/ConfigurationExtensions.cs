using Synology.Photos.Slideshow.Api.Configuration;
using Synology.Photos.Slideshow.Api.Constants;
using Synology.Photos.Slideshow.Api.Slideshow.Auth;
using Synology.Photos.Slideshow.Api.Slideshow.Providers;
using Synology.Photos.Slideshow.Api.Slideshow.Services;

namespace Synology.Photos.Slideshow.Api.Extensions;

public static class ConfigurationExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection ConfigureServices(ConfigurationManager configuration)
        {
            services.ConfigureOptions(configuration);
            services.AddHttpContextAccessor();
            
            services
                .AddHttpClient(SlideshowConstants.GeolocationHttpClient)
                .AddStandardResilienceHandler();
        
            services.AddScoped<ISynologyAuthenticationContext, SynologyAuthenticationContext>();
            services.AddSingleton<ISynologyApiInfoProvider, SynologyApiInfoProvider>();
            services.AddTransient<INasPhotoSearchService, NasPhotoSearchService>();
            services.AddTransient<IFileStation, FileStation>();
            services.AddTransient<IFileProcessor, FileProcessor>();
            services.AddTransient<IPhotosService, PhotosService>();
            services.AddTransient<ILocationService, GoogleLocationService>();
  
            return services;
        }

        private void ConfigureOptions(ConfigurationManager configuration)
        {
            services
                .AddOptionsWithValidateOnStart<SynologyUser>()
                .ValidateDataAnnotations()
                .Bind(configuration.GetSection(nameof(SynologyUser)));
        
            services
                .AddOptionsWithValidateOnStart<SynoApiOptions>()
                .ValidateDataAnnotations()
                .Bind(configuration.GetSection(nameof(SynoApiOptions)));
            
            services
                .AddOptionsWithValidateOnStart<ThirdPartyServices>()
                .ValidateDataAnnotations()
                .Bind(configuration.GetSection(nameof(ThirdPartyServices)));
            
            services
                .AddOptionsWithValidateOnStart<GoogleMapsOptions>()
                .ValidateDataAnnotations()
                .Bind(configuration.GetSection(nameof(GoogleMapsOptions)));
        }
    }
}