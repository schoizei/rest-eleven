using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Options;
using RestEleven.Client.Options;

namespace RestEleven.Client.Services;

public class PushServerAuthorizationMessageHandler : AuthorizationMessageHandler
{
    public PushServerAuthorizationMessageHandler(IAccessTokenProvider provider, NavigationManager navigation, IOptionsSnapshot<ClientOptions> options)
        : base(provider, navigation)
    {
        var clientOptions = options.Value;
        ConfigureHandler(
            authorizedUrls: new[] { clientOptions.PushServerBaseUrl.ToString().TrimEnd('/') },
            scopes: clientOptions.PushServerScopes);
    }
}
