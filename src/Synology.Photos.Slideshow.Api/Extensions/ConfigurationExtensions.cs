using Microsoft.Extensions.Caching.Hybrid;
using Synology.Photos.Slideshow.Api.Configuration;
using Synology.Photos.Slideshow.Api.Constants;
using Synology.Photos.Slideshow.Api.Slideshow.Auth;
using Synology.Photos.Slideshow.Api.Slideshow.Providers;
using Synology.Photos.Slideshow.Api.Slideshow.Services;
using Synology.Photos.Slideshow.Api.Slideshow.Services.Mocks;

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
            
            services.ConfigureRedis(configuration);
            
            services.AddHybridCache(options =>
            {
                var cacheDuration = TimeSpan.FromDays(7);
                
                options.DefaultEntryOptions = new HybridCacheEntryOptions
                {
                    Expiration = cacheDuration,
                    LocalCacheExpiration = cacheDuration
                };
            });
        
            services.AddScoped<ISynologyAuthenticationContext, SynologyAuthenticationContext>();
            services.AddSingleton<ISynologyApiInfoProvider, SynologyApiInfoProvider>();
            services.AddTransient<INasPhotoSearchService, NasPhotoSearchService>();
            services.AddTransient<IFileStation, FileStation>();
            services.AddTransient<IFileProcessor, FileProcessor>();
            services.AddTransient<IPhotosService, PhotosService>();
            services.ConfigureLocationService(configuration);
            
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
        
        private void ConfigureLocationService(ConfigurationManager configuration)
        {
#if DEBUG
            var googleMapsOptions = configuration
                .GetSection(nameof(GoogleMapsOptions))
                .Get<GoogleMapsOptions>();

            if (googleMapsOptions!.EnableMocks)
                services.AddTransient<ILocationService, GoogleLocationServiceMock>();
            else
                services.AddTransient<ILocationService, GoogleLocationService>();
#else
            services.AddTransient<ILocationService, GoogleLocationService>();
#endif
        }

        private void ConfigureRedis(ConfigurationManager configuration)
        {
            var thirdPartyServices = configuration
                .GetSection(nameof(ThirdPartyServices))
                .Get<ThirdPartyServices>();

            if (thirdPartyServices!.EnableDistributedCache)
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = configuration.GetConnectionString("Redis");
                });
            }
        }
    }
}