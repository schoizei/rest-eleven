using System.Globalization;
using System.Text.Json;
using Microsoft.JSInterop;
using RestEleven.Client.Options;
using RestEleven.Shared.Models;

namespace RestEleven.Client.Services;

public class SettingsService : ISettingsService
{
    private const string StorageKey = "resteleven-settings";
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly IJSRuntime _jsRuntime;
    private readonly ClientOptions _clientOptions;

    public SettingsService(IJSRuntime jsRuntime, ClientOptions clientOptions)
    {
        _jsRuntime = jsRuntime;
        _clientOptions = clientOptions;
    }

    public async Task<SettingsState> GetAsync(CancellationToken cancellationToken = default)
    {
        var stored = await _jsRuntime.InvokeAsync<string?>("resteleven.storage.get", cancellationToken, StorageKey);
        if (string.IsNullOrWhiteSpace(stored))
        {
            return BuildDefaultState();
        }

        var payload = JsonSerializer.Deserialize<SerializedState>(stored, SerializerOptions);
        if (payload is null)
        {
            return BuildDefaultState();
        }

        return payload.ToState();
    }

    public async Task SaveAsync(SettingsState state, CancellationToken cancellationToken = default)
    {
        var payload = SerializedState.FromState(state);
        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        await _jsRuntime.InvokeVoidAsync("resteleven.storage.set", cancellationToken, StorageKey, json);
    }

    private SettingsState BuildDefaultState()
    {
        var reminder = new ReminderPreference
        {
            Enabled = _clientOptions.ReminderDefaults.Enabled,
            PreferredTime = TimeOnly.Parse(_clientOptions.ReminderDefaults.PreferredTime, CultureInfo.InvariantCulture),
            LeadMinutes = _clientOptions.ReminderDefaults.LeadMinutes
        };

        return new SettingsState(
            Reminder: reminder,
            Alpha: 0.3,
            PersonioBridgeUrl: _clientOptions.PersonioBridgeBaseUrl,
            PushOptIn: false,
            AutoSubmitToPersonio: false);
    }

    private sealed record SerializedState(ReminderPreference Reminder, double Alpha, string PersonioBridgeUrl, bool PushOptIn, bool AutoSubmitToPersonio)
    {
        public SettingsState ToState()
        {
            return new SettingsState(
                Reminder,
                Alpha,
                new Uri(PersonioBridgeUrl, UriKind.Absolute),
                PushOptIn,
                AutoSubmitToPersonio);
        }

        public static SerializedState FromState(SettingsState state)
        {
            return new SerializedState(state.Reminder, state.Alpha, state.PersonioBridgeUrl.ToString(), state.PushOptIn, state.AutoSubmitToPersonio);
        }
    }
}
