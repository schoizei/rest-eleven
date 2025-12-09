using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestEleven.PushServer.Options;
using RestEleven.Shared.Dtos;
using RestEleven.Shared.Results;
using WebPush;

namespace RestEleven.PushServer.Services;

public partial class WebPushService : IWebPushService
{
    private readonly IPushSubscriptionStore _store;
    private readonly VapidOptions _options;
    private readonly WebPushClient _client;
    private readonly ILogger<WebPushService> _logger;

    public WebPushService(IPushSubscriptionStore store, IOptions<VapidOptions> options, WebPushClient client, ILogger<WebPushService> logger)
    {
        _store = store;
        _client = client;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<SimpleResult<int>> BroadcastAsync(NotificationDto notification, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.PublicKey) || string.IsNullOrWhiteSpace(_options.PrivateKey))
        {
            return SimpleResult<int>.Failure("VAPID keys are not configured");
        }

        var subscriptions = await _store.ListAsync(cancellationToken);
        if (subscriptions.Count == 0)
        {
            return SimpleResult<int>.Success(0);
        }

        var payload = JsonSerializer.Serialize(new
        {
            title = notification.Title,
            body = notification.Body,
            url = notification.Url
        });

        var vapid = new VapidDetails(_options.Subject, _options.PublicKey, _options.PrivateKey);
        var delivered = 0;
        var failures = new List<string>();

        foreach (var subscription in subscriptions)
        {
            try
            {
                var pushSubscription = new PushSubscription(subscription.Endpoint, subscription.P256dh, subscription.Auth);
                await _client.SendNotificationAsync(pushSubscription, payload, vapid, cancellationToken);
                delivered++;
            }
            catch (WebPushException ex) when (ex.StatusCode == HttpStatusCode.Gone || ex.StatusCode == HttpStatusCode.NotFound)
            {
                await _store.RemoveAsync(subscription.Endpoint, cancellationToken);
                LogExpiredSubscription(_logger, ex, subscription.Endpoint);
            }
            catch (Exception ex)
            {
                LogPushSendFailure(_logger, ex);
                failures.Add(ex.Message);
            }
        }

        if (failures.Count > 0)
        {
            return SimpleResult<int>.Failure(failures.ToArray());
        }

        return SimpleResult<int>.Success(delivered);
    }
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Removed expired subscription: {Endpoint}")]
    private static partial void LogExpiredSubscription(ILogger logger, Exception exception, string Endpoint);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Failed to send push notification")]
    private static partial void LogPushSendFailure(ILogger logger, Exception exception);
}
