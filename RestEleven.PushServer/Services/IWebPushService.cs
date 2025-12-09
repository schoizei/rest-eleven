using RestEleven.Shared.Dtos;
using RestEleven.Shared.Results;

namespace RestEleven.PushServer.Services;

public interface IWebPushService
{
    Task<SimpleResult<int>> BroadcastAsync(NotificationDto notification, CancellationToken cancellationToken = default);
}
