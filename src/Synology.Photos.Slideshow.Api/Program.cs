using Serilog;
using Synology.Api.Sdk.Config;
using Synology.Photos.Slideshow.Api.DI;
using Synology.Photos.Slideshow.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;
configuration.GetApiConfigurations();

var serilog = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();
Log.Logger = serilog;

var services = builder.Services;
services
    .ConfigureServices(configuration)
    .AddSerilog(serilog)
    .AddOpenApi()
    .ConfigureSynologyApiSdkDependencies(configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{ 
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseSynologyAuthentication();
app.ConfigureEndpoints();
app.Run();
