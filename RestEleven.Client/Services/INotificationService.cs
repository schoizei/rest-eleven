using RestEleven.Shared.Models;

namespace RestEleven.Client.Services;

public interface INotificationService
{
    Task<bool> EnsurePermissionAsync(CancellationToken cancellationToken = default);
    Task<bool> RegisterPushSubscriptionAsync(string? clientId, CancellationToken cancellationToken = default);
    Task ShowLocalNotificationAsync(string title, string body, string? url = null, CancellationToken cancellationToken = default);
    Task ScheduleReminderAsync(ReminderPreference preference, LearningSuggestion? suggestion, CancellationToken cancellationToken = default);
}
