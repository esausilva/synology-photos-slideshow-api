using Microsoft.Extensions.Options;
using Synology.Api.Sdk.Constants;
using Synology.Api.Sdk.SynologyApi;
using Synology.Api.Sdk.SynologyApi.Auth.Request;
using Synology.Api.Sdk.SynologyApi.Auth.Response;
using Synology.Photos.Slideshow.Api.Configuration;
using Synology.Photos.Slideshow.Api.Slideshow.Common;

namespace Synology.Photos.Slideshow.Api.Middleware;

public sealed class SynologyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ISynologyApiService _synoApiService;
    private readonly ISynologyApiRequestBuilder _synoApiRequestBuilder;
    private readonly ISynologyApiInfo _synoApiInfo;
    private readonly SynologyUser _user;
    private readonly ILogger<SynologyAuthenticationMiddleware> _logger;

    public SynologyAuthenticationMiddleware(
        RequestDelegate next,
        ISynologyApiService synoApiService,
        ISynologyApiRequestBuilder synoApiRequestBuilder,
        ISynologyApiInfo synoApiInfo,
        IOptions<SynologyUser> synologyUser,
        ILogger<SynologyAuthenticationMiddleware> logger)
    {
        _next = next;
        _synoApiService = synoApiService;
        _synoApiRequestBuilder = synoApiRequestBuilder;
        _synoApiInfo = synoApiInfo;
        _user = synologyUser.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;
        
        if (path is { Value: "/download-photos" })
        {
            try
            {
                var loginResponse = await AuthenticateAsync(context.RequestAborted);

                // Store login response in a custom feature class for later use
                context.Features.Set(new SynologyAuthenticationFeature(loginResponse));

                await _next(context);
            }
            finally
            {
                // Always attempt to log out when the request is complete
                if (context.Features.Get<SynologyAuthenticationFeature>() is { } authFeature)
                {
                    await LogoutAsync(authFeature.LoginResponse, context.RequestAborted);
                }
            }
        }
        
        await _next(context);
    }

    private async Task<LoginResponse> AuthenticateAsync(CancellationToken cancellationToken)
    {
        try
        {
            var apiVersionInfo = await _synoApiInfo.GetApiInfo(cancellationToken);
            var loginRequest = new LoginRequest(
                method: SynologyApiMethods.Api.Auth_Login,
                version: apiVersionInfo.SynoApiAuth.MaxVersion,
                account: _user.Account,
                password: _user.Password);
            var loginUrl = _synoApiRequestBuilder.BuildUrl(loginRequest);
        
            _logger.LogDebug("Authenticating with Synology API");
            
            var loginResponse = await _synoApiService.GetAsync<LoginResponse>(loginUrl, cancellationToken);
            
            return string.IsNullOrWhiteSpace(loginResponse.Data.SynoToken) 
                ? throw new SynologyAuthenticationException("SynoToken is null or empty") 
                : loginResponse;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to login to Synology API");
            throw;
        }
    }

    private async Task LogoutAsync(LoginResponse loginResponse, CancellationToken cancellationToken)
    {
        try
        {
            var apiVersionInfo = await _synoApiInfo.GetApiInfo(cancellationToken);
            var logoutRequest = new LogoutRequest(
                method: SynologyApiMethods.Api.Auth_Logout,
                version: apiVersionInfo.SynoApiAuth.MaxVersion,
                sid: loginResponse.Data.Sid);
            var logoutUrl = _synoApiRequestBuilder.BuildUrl(logoutRequest);
            
            _logger.LogDebug("Logging out from Synology API");
            await _synoApiService.GetAsync<LogoutResponse>(logoutUrl, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Failed to logout from Synology API");
        }
    }
}

public sealed class SynologyAuthenticationFeature(LoginResponse loginResponse)
{
    public LoginResponse LoginResponse { get; } = loginResponse;
}

public sealed class SynologyAuthenticationException(string message) : Exception(message);
