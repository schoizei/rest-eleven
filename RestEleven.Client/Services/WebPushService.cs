using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using RestEleven.Client.Options;
using RestEleven.Shared.Dtos;
using RestEleven.Shared.Models;

namespace RestEleven.Client.Services;

public partial class WebPushService : INotificationService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ClientOptions _options;
    private readonly ILogger<WebPushService> _logger;

    public WebPushService(IJSRuntime jsRuntime, IHttpClientFactory httpClientFactory, ClientOptions options, ILogger<WebPushService> logger)
    {
        _jsRuntime = jsRuntime;
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public async Task<bool> EnsurePermissionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var state = await _jsRuntime.InvokeAsync<string>("resteleven.notifications.ensurePermission", cancellationToken);
            return string.Equals(state, "granted", StringComparison.OrdinalIgnoreCase);
        }
        catch (JSDisconnectedException)
        {
            return false;
        }
    }

    public async Task<bool> RegisterPushSubscriptionAsync(string? clientId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.VapidPublicKey))
        {
            LogMissingVapidKey(_logger);
            return false;
        }

        var permission = await EnsurePermissionAsync(cancellationToken);
        if (!permission)
        {
            return false;
        }

        PushSubscriptionPayload? payload = null;
        try
        {
            payload = await _jsRuntime.InvokeAsync<PushSubscriptionPayload?>("resteleven.notifications.subscribe", cancellationToken, _options.VapidPublicKey);
        }
        catch (JSException ex)
        {
            LogSubscriptionError(_logger, ex);
            return false;
        }

        if (payload?.Endpoint is null)
        {
            return false;
        }

        var dto = new SubscriptionDto
        {
            Endpoint = payload.Endpoint,
            P256dh = payload.Keys.P256dh,
            Auth = payload.Keys.Auth,
            ClientId = clientId
        };

        var httpClient = _httpClientFactory.CreateClient("PushServer");
        var response = await httpClient.PostAsJsonAsync("push/subscribe", dto, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task ShowLocalNotificationAsync(string title, string body, string? url = null, CancellationToken cancellationToken = default)
    {
        await _jsRuntime.InvokeVoidAsync("resteleven.notifications.show", cancellationToken, new
        {
            title,
            body,
            url
        });
    }

    public async Task ScheduleReminderAsync(ReminderPreference preference, LearningSuggestion? suggestion, CancellationToken cancellationToken = default)
    {
        if (!preference.Enabled)
        {
            return;
        }

        var reminderTime = preference.PreferredTime.AddMinutes(-preference.LeadMinutes);
        var message = suggestion is null
            ? "Erinnerung an deine Zeiterfassung"
            : $"Nächster Vorschlag {suggestion.SuggestedStart:HH\\:mm} ({suggestion.Confidence:P0})";

        await _jsRuntime.InvokeVoidAsync("resteleven.notifications.scheduleReminder", cancellationToken, new
        {
            hour = reminderTime.Hour,
            minute = reminderTime.Minute,
            title = "RestEleven",
            body = message,
            url = "/"
        });
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "VAPID public key missing; skip subscription registration")]
    private static partial void LogMissingVapidKey(ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Failed to subscribe to push manager")]
    private static partial void LogSubscriptionError(ILogger logger, Exception exception);

    private sealed record PushSubscriptionPayload(string Endpoint, PushKeys Keys);
    private sealed record PushKeys(string P256dh, string Auth);
}
