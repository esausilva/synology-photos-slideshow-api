using Synology.Photos.Slideshow.Api.Slideshow.Auth;

namespace Synology.Photos.Slideshow.Api.Slideshow.DownloadPhotos.Endpoints;

public static class DownloadPhotos
{
    public static async Task<IResult> GetAsync(
        HttpContext context,
        ISynologyAuthenticationContext synologyAuthentication,
        CancellationToken cancellationToken)
    {
        var loginResponse = synologyAuthentication.GetLoginResponse();
        
        var summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast(
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        
        return Results.Ok(forecast);
    }
}

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
