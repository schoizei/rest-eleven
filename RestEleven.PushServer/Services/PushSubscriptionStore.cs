using System.Collections.Concurrent;
using RestEleven.Shared.Dtos;

namespace RestEleven.PushServer.Services;

public class PushSubscriptionStore : IPushSubscriptionStore
{
    private readonly ConcurrentDictionary<string, SubscriptionDto> _subscriptions = new(StringComparer.Ordinal);

    public Task UpsertAsync(SubscriptionDto subscription, CancellationToken cancellationToken = default)
    {
        _subscriptions.AddOrUpdate(subscription.Endpoint, subscription, (_, _) => subscription);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<SubscriptionDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<SubscriptionDto> snapshot = _subscriptions.Values.ToList();
        return Task.FromResult(snapshot);
    }

    public Task RemoveAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        _subscriptions.TryRemove(endpoint, out _);
        return Task.CompletedTask;
    }
}
