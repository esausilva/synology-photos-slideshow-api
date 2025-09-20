using Synology.Api.Sdk.SynologyApi.Auth.Response;

namespace Synology.Photos.Slideshow.Api.Slideshow.Auth;

public interface ISynologyAuthenticationContext
{
    LoginResponse GetLoginResponse();
    string? GetSynoToken();
}