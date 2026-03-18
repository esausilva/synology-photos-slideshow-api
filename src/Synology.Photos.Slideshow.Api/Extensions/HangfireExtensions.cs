using Hangfire;
using Microsoft.Extensions.Options;
using Synology.Photos.Slideshow.Api.Configuration;
using Synology.Photos.Slideshow.Api.Slideshow.Jobs;

namespace Synology.Photos.Slideshow.Api.Extensions;

public static class HangfireExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddHangfireServices()
        {
            services.AddHangfire(config =>
            {
                config
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseInMemoryStorage();
            });

            services.AddHangfireServer();
            services.AddTransient<PhotoDownloadJob>();
            
            return services;
        }
    }
    
    extension(IApplicationBuilder app)
    {
        public IApplicationBuilder UseHangfireScheduling()
        {
            app.UseHangfireDashboard("/jobs");
            
            var serviceProvider = app.ApplicationServices;
            var jobOptions = serviceProvider.GetRequiredService<IOptions<PhotoDownloadJobOptions>>().Value;
            // var schedule = Cron.Weekly((DayOfWeek)jobOptions.DayOfWeek, jobOptions.Hour, jobOptions.Minute);
            var schedule = Cron.Weekly(DayOfWeek.Tuesday, 7, 24);
            
            RecurringJob.AddOrUpdate<PhotoDownloadJob>(
                "photo-download-job",
                job => job.Execute(CancellationToken.None),
                schedule,
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Local
                });
            
            return app;
        }
    }
}
