using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Options;
using RestEleven.Client.Options;

namespace RestEleven.Client.Services;

public class PersonioBridgeAuthorizationMessageHandler : AuthorizationMessageHandler
{
    public PersonioBridgeAuthorizationMessageHandler(IAccessTokenProvider provider, NavigationManager navigation, IOptionsSnapshot<ClientOptions> options)
        : base(provider, navigation)
    {
        var clientOptions = options.Value;
        ConfigureHandler(
            authorizedUrls: new[] { clientOptions.PersonioBridgeBaseUrl.ToString().TrimEnd('/') },
            scopes: clientOptions.PersonioBridgeScopes);
    }
}
