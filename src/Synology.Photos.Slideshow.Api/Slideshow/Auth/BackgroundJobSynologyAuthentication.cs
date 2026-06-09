using Microsoft.Extensions.Options;
using Synology.Api.Sdk.Constants;
using Synology.Api.Sdk.SynologyApi;
using Synology.Api.Sdk.SynologyApi.Auth.Request;
using Synology.Api.Sdk.SynologyApi.Auth.Response;
using Synology.Photos.Slideshow.Api.Configuration;
using Synology.Photos.Slideshow.Api.Middleware;
using Synology.Photos.Slideshow.Api.Slideshow.Providers;

namespace Synology.Photos.Slideshow.Api.Slideshow.Auth;

public sealed class BackgroundJobSynologyAuthentication : IBackgroundJobSynologyAuthentication
{
    private readonly ISynologyApiClient _synologyApiClient;
    private readonly ISynologyApiInfoProvider _apiInfoProvider;
    private readonly SynologyUser _user;
    private readonly ILogger<BackgroundJobSynologyAuthentication> _logger;
    
    private LoginResponse? _loginResponse;

    public BackgroundJobSynologyAuthentication(
        ISynologyApiClient synologyApiClient,
        ISynologyApiInfoProvider apiInfoProvider,
        IOptions<SynologyUser> synologyUser,
        ILogger<BackgroundJobSynologyAuthentication> logger)
    {
        _synologyApiClient = synologyApiClient;
        _apiInfoProvider = apiInfoProvider;
        _user = synologyUser.Value;
        _logger = logger;
    }

    public async Task AuthenticateAsync(CancellationToken cancellationToken)
    {
        var apiVersionInfo = await _apiInfoProvider.GetOrFetchInfo(cancellationToken);
        var loginRequest = new LoginRequest(
            method: SynologyApiMethods.Api.Auth_Login,
            version: apiVersionInfo.SynoApiAuth.MaxVersion,
            account: _user.Account,
            password: _user.Password);

        _logger.LogDebug("Background job: authenticating with Synology API");

        _loginResponse = await _synologyApiClient.AuthApi.LoginAsync(loginRequest, cancellationToken);

        if (string.IsNullOrWhiteSpace(_loginResponse.Data.SynoToken))
            throw new SynologyAuthenticationException("Background job: SynoToken is null or empty");
    }

    public string? GetSynoToken() => _loginResponse?.Data.SynoToken;

    public async ValueTask DisposeAsync()
    {
        if (_loginResponse is null)
            return;

        try
        {
            var apiVersionInfo = await _apiInfoProvider.GetOrFetchInfo(CancellationToken.None);
            var logoutRequest = new LogoutRequest(
                method: SynologyApiMethods.Api.Auth_Logout,
                version: apiVersionInfo.SynoApiAuth.MaxVersion,
                sid: _loginResponse.Data.Sid);

            _logger.LogDebug("Background job: logging out from Synology API");
            await _synologyApiClient.AuthApi.LogoutAsync(logoutRequest, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Background job: failed to logout from Synology API");
        }
    }
}