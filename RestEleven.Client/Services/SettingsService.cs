using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using RestEleven.Client.Options;
using RestEleven.Shared.Models;
using RestEleven.Shared.Dtos;

namespace RestEleven.Client.Services;

public class SettingsService : ISettingsService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ClientOptions _clientOptions;
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(IHttpClientFactory httpClientFactory, ClientOptions clientOptions, ILogger<SettingsService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _clientOptions = clientOptions;
        _logger = logger;
    }

    public async Task<SettingsState> GetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("PushServer");
            var dto = await client.GetFromJsonAsync<UserSettingsDto>("settings/me", cancellationToken);
            return dto is null ? BuildDefaultState() : MapToState(dto);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return BuildDefaultState();
        }
        catch (Exception ex)
        {
            Log.UserSettingsLoadFailed(_logger, ex);
            return BuildDefaultState();
        }
    }

    public async Task SaveAsync(SettingsState state, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("PushServer");
            var dto = MapToDto(state);
            var response = await client.PutAsJsonAsync("settings/me", dto, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Log.UserSettingsPersistFailed(_logger, ex);
            throw;
        }
    }

    private static class Log
    {
        private static readonly Action<ILogger, Exception?> _userSettingsLoadFailed = LoggerMessage.Define(
            LogLevel.Error,
            new EventId(1, nameof(UserSettingsLoadFailed)),
            "Failed to load user settings");

        private static readonly Action<ILogger, Exception?> _userSettingsPersistFailed = LoggerMessage.Define(
            LogLevel.Error,
            new EventId(2, nameof(UserSettingsPersistFailed)),
            "Failed to persist user settings");

        public static void UserSettingsLoadFailed(ILogger logger, Exception exception) => _userSettingsLoadFailed(logger, exception);

        public static void UserSettingsPersistFailed(ILogger logger, Exception exception) => _userSettingsPersistFailed(logger, exception);
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

    private static SettingsState MapToState(UserSettingsDto dto) => new(
        dto.Reminder,
        dto.Alpha,
        new Uri(dto.PersonioBridgeUrl, UriKind.Absolute),
        dto.PushOptIn,
        dto.AutoSubmitToPersonio);

    private static UserSettingsDto MapToDto(SettingsState state) => new(
        state.Reminder,
        state.Alpha,
        state.PersonioBridgeUrl.ToString(),
        state.PushOptIn,
        state.AutoSubmitToPersonio);
}
