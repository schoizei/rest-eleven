namespace RestEleven.Client.Services;

public interface IBackgroundSyncService
{
    Task RegisterAsync(string tag, CancellationToken cancellationToken = default);
    Task<bool> IsSupportedAsync(CancellationToken cancellationToken = default);
}
