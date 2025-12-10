using RestEleven.Shared.Models;

namespace RestEleven.Shared.Dtos;

public record UserSettingsDto(
    ReminderPreference Reminder,
    double Alpha,
    string PersonioBridgeUrl,
    bool PushOptIn,
    bool AutoSubmitToPersonio);
