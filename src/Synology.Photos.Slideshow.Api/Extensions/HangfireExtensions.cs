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
            var serviceProvider = app.ApplicationServices;
            
            app.UseHangfireDashboard("/jobs", new DashboardOptions()
            {
                // If this was a client facing production, we'd want a real auth filter here,
                // but this API is meant to be run on a local network in Docker
                Authorization = []
            });
            app.ConfigurePhotoDownloadJob(serviceProvider);
            
            return app;
        }

        private IApplicationBuilder ConfigurePhotoDownloadJob(IServiceProvider serviceProvider)
        {
            var jobOptions = serviceProvider.GetRequiredService<IOptions<PhotoDownloadScheduledJobOptions>>().Value;
            
            if (!jobOptions.Enabled)
                return app;
            
            var cronSchedule = Cron.Weekly((DayOfWeek)jobOptions.DayOfWeek, jobOptions.Hour, jobOptions.Minute);
            
            // For local testing -- uncomment
            // cronSchedule = Cron.Weekly(DayOfWeek.Wednesday, 20, 45);
            // cronSchedule = Cron.MinuteInterval(5);

            RecurringJob.AddOrUpdate<PhotoDownloadJob>(
                "photo-download-job",
                job => job.Execute(CancellationToken.None),
                cronSchedule,
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.FindSystemTimeZoneById(jobOptions.TimeZoneId)
                });
            
            return app;
        }
    }
}
