using Microsoft.AspNetCore.Http;
using NSubstitute;
using Synology.Api.Sdk.SynologyApi.Auth.Response;
using Synology.Photos.Slideshow.Api.Middleware;
using Synology.Photos.Slideshow.Api.Slideshow.Auth;

namespace Synology.Photos.Slideshow.Api.Tests.UnitTests.AuthTests;

public class SynologyAuthenticationContextTests
{
    private static LoginResponse BuildLoginResponse(string? synoToken = "syno-token", string sid = "sid")
    {
        return new LoginResponse
        {
            Data = new LoginData
            {
                SynoToken = synoToken,
                Sid = sid,
                Account = "admin",
            },
        };
    }

    [Test]
    public async Task Assert_GetLoginResponse_Returns_Feature_Value_When_Present()
    {
        var loginResponse = BuildLoginResponse();
        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set(new SynologyAuthenticationFeature(loginResponse));

        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        var context = new SynologyAuthenticationContext(accessor);
        var result = context.GetLoginResponse();

        await Assert
            .That(result)
            .IsSameReferenceAs(loginResponse);
    }

    [Test]
    public async Task Assert_GetLoginResponse_Throws_When_Feature_Is_Missing()
    {
        var httpContext = new DefaultHttpContext();
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        var context = new SynologyAuthenticationContext(accessor);

        await Assert
            .That(context.GetLoginResponse)
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task Assert_GetLoginResponse_Throws_When_HttpContext_Is_Null()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);

        var context = new SynologyAuthenticationContext(accessor);

        await Assert
            .That(context.GetLoginResponse)
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task Assert_GetSynoToken_Returns_SynoToken_From_LoginResponse()
    {
        var loginResponse = BuildLoginResponse(synoToken: "expected-syno-token");
        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set(new SynologyAuthenticationFeature(loginResponse));

        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        var context = new SynologyAuthenticationContext(accessor);
        var token = context.GetSynoToken();

        await Assert
            .That(token)
            .IsEqualTo("expected-syno-token");
    }

    [Test]
    public async Task Assert_GetSynoToken_Returns_Null_When_SynoToken_Is_Null()
    {
        var loginResponse = BuildLoginResponse(synoToken: null);
        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set(new SynologyAuthenticationFeature(loginResponse));

        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        var context = new SynologyAuthenticationContext(accessor);
        var token = context.GetSynoToken();

        await Assert
            .That(token)
            .IsNull();
    }
}
