using RestEleven.Shared.Models;

namespace RestEleven.Client.Services;

public record SettingsState(ReminderPreference Reminder, double Alpha, Uri PersonioBridgeUrl, bool PushOptIn, bool AutoSubmitToPersonio);

public interface ISettingsService
{
    Task<SettingsState> GetAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(SettingsState state, CancellationToken cancellationToken = default);
}
