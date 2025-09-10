using Synology.Api.Sdk.SynologyApi.Auth.Response;
using Synology.Photos.Slideshow.Api.Middleware;

namespace Synology.Photos.Slideshow.Api.Slideshow.Auth;

public sealed class SynologyAuthenticationContext : ISynologyAuthenticationContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SynologyAuthenticationContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public LoginResponse GetLoginResponse()
    {
        var feature = _httpContextAccessor.HttpContext?.Features.Get<SynologyAuthenticationFeature>();
        
        return feature == null 
            ? throw new InvalidOperationException("Synology authentication feature not found. Make sure the SynologyAuthenticationMiddleware is configured correctly.") 
            : feature.LoginResponse;
    }

    public string GetSid()
    {
        return GetLoginResponse().Data.Sid;
    }
}
