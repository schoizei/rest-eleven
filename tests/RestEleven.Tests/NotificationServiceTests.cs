using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using RestEleven.Client.Options;
using RestEleven.Client.Services;
using Xunit;

namespace RestEleven.Tests;

public class NotificationServiceTests
{
    [Fact]
    public async Task EnsurePermissionAsync_ReturnsFalse_WhenJsDisconnected()
    {
        var service = CreateService(new ThrowingJsRuntime(), options => options.VapidPublicKey = "test-key");

        var result = await service.EnsurePermissionAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task RegisterPushSubscriptionAsync_ReturnsFalse_WhenVapidMissing()
    {
        var service = CreateService(new NoopJsRuntime(), options => options.VapidPublicKey = string.Empty);
        var result = await service.RegisterPushSubscriptionAsync(null);
        Assert.False(result);
    }

    private static WebPushService CreateService(IJSRuntime jsRuntime, Action<ClientOptions>? configure = null)
    {
        var options = new ClientOptions();
        configure?.Invoke(options);

        var httpClient = new HttpClient(new FakeHandler())
        {
            BaseAddress = new Uri("https://localhost")
        };

        var factory = new FixedHttpClientFactory(httpClient);
        return new WebPushService(jsRuntime, factory, options, NullLogger<WebPushService>.Instance);
    }

    private sealed class ThrowingJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            throw new JSDisconnectedException("JS runtime unavailable");
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            throw new JSDisconnectedException("JS runtime unavailable");
        }
    }

    private sealed class NoopJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            return ValueTask.FromResult(default(TValue)!);
        }
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        }
    }

    private sealed class FixedHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public FixedHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name)
        {
            return _client;
        }
    }
}
