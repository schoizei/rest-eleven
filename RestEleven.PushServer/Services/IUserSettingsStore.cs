using RestEleven.Shared.Dtos;

namespace RestEleven.PushServer.Services;

public interface IUserSettingsStore
{
    Task<UserSettingsDto?> GetAsync(string userId, CancellationToken cancellationToken = default);
    Task SaveAsync(string userId, UserSettingsDto settings, CancellationToken cancellationToken = default);
}
