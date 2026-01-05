using Microsoft.AspNetCore.Cors.Infrastructure;
using Synology.Photos.Slideshow.Api.Constants;

namespace Synology.Photos.Slideshow.Api.Security;

public static class Cors
{
    public const string FrontEndPolicyName = "AllowFrontEnd";

    public static Action<CorsOptions> Configure()
    {
        return options =>
        {
            options
                .AddPolicy(name: FrontEndPolicyName,
                    policy =>
                    {
                        policy
                            .AllowAnyOrigin()
                            .AllowAnyHeader();
                        policy.WithMethods(HttpVerbs.Post, HttpVerbs.Get);
                    }
                );
        };
    }
}
