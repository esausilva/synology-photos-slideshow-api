namespace Synology.Photos.Slideshow.Api.Slideshow.Auth;

public interface IBackgroundJobSynologyAuthentication : IAsyncDisposable
{
    Task AuthenticateAsync(CancellationToken cancellationToken);
    string? GetSynoToken();
}