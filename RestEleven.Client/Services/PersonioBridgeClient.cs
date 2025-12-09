using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using RestEleven.Shared.Dtos;
using RestEleven.Shared.Results;

namespace RestEleven.Client.Services;

public partial class PersonioBridgeClient : IPersonioBridgeClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PersonioBridgeClient> _logger;

    public PersonioBridgeClient(IHttpClientFactory httpClientFactory, ILogger<PersonioBridgeClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<SimpleResult<AttendanceDto>> CreateAttendanceAsync(CreateAttendanceDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("PersonioBridge");
            var response = await client.PostAsJsonAsync("personio/attendance", dto, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var attendance = await response.Content.ReadFromJsonAsync<AttendanceDto>(cancellationToken: cancellationToken);
                if (attendance is not null)
                {
                    return SimpleResult<AttendanceDto>.Success(attendance);
                }
            }

            var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsDto>(cancellationToken: cancellationToken);
            var errors = problem?.Errors.SelectMany(pair => pair.Value).ToArray();
            return SimpleResult<AttendanceDto>.Failure(errors is { Length: > 0 } ? errors : new[] { problem?.Detail ?? "Unbekannter Fehler" });
        }
        catch (HttpRequestException ex)
        {
            LogBridgeError(_logger, ex);
            return SimpleResult<AttendanceDto>.Failure("Verbindung zur PersonioBridge fehlgeschlagen");
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Failed to reach PersonioBridge API")]
    private static partial void LogBridgeError(ILogger logger, Exception exception);
}
