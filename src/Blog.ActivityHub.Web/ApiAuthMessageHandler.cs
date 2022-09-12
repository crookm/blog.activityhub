using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Blog.ActivityHub.Web;

public class ApiAuthMessageHandler : AuthorizationMessageHandler
{
    public ApiAuthMessageHandler(IAccessTokenProvider provider, NavigationManager navigation)
        : base(provider, navigation)
    {
        ConfigureHandler(authorizedUrls: new[] { "https://activityhub-api.mattcrook.io", "https://localhost:7279" });
    }
}