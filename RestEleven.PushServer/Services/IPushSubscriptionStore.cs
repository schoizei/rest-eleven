using RestEleven.Shared.Dtos;

namespace RestEleven.PushServer.Services;

public interface IPushSubscriptionStore
{
    Task UpsertAsync(SubscriptionDto subscription, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<SubscriptionDto>> ListAsync(CancellationToken cancellationToken = default);
    Task RemoveAsync(string endpoint, CancellationToken cancellationToken = default);
}
