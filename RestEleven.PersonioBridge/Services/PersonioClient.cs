using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestEleven.PersonioBridge.Options;
using RestEleven.Shared.Dtos;
using RestEleven.Shared.Results;

namespace RestEleven.PersonioBridge.Services;

public partial class PersonioClient : IPersonioClient
{
    private readonly HttpClient _httpClient;
    private readonly PersonioOptions _options;
    private readonly ILogger<PersonioClient> _logger;
    private string? _accessToken;
    private DateTimeOffset _tokenExpiresAt;

    public PersonioClient(HttpClient httpClient, IOptions<PersonioOptions> options, ILogger<PersonioClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SimpleResult<AttendanceDto>> CreateAttendanceAsync(CreateAttendanceDto request, CancellationToken cancellationToken = default)
    {
        var token = await EnsureTokenAsync(cancellationToken);
        if (token is null)
        {
            return SimpleResult<AttendanceDto>.Failure("Authentifizierung bei Personio fehlgeschlagen");
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _options.AttendanceUrl)
        {
            Content = JsonContent.Create(new
            {
                attendances = new[]
                {
                    new
                    {
                        employee = new { id = _options.EmployeeId },
                        date = request.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        start_time = request.Start.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                        end_time = request.End.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                        @break = request.BreakMinutes,
                        comment = request.Comment
                    }
                }
            })
        };

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var dto = new AttendanceDto
            {
                Id = Guid.NewGuid(),
                Date = request.Date,
                Start = request.Start,
                End = request.End,
                BreakMinutes = request.BreakMinutes,
                Comment = request.Comment,
                Confidence = 1,
                SubmittedUtc = DateTime.UtcNow
            };

            return SimpleResult<AttendanceDto>.Success(dto);
        }

        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (_logger.IsEnabled(LogLevel.Error))
        {
            LogPersonioResponseError(_logger, response.StatusCode, errorBody);
        }
        return SimpleResult<AttendanceDto>.Failure($"Personio Antwort {response.StatusCode}", errorBody);
    }

    private async Task<string?> EnsureTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_accessToken) && _tokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            return _accessToken;
        }

        var payload = new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret
        };

        using var authRequest = new HttpRequestMessage(HttpMethod.Post, _options.AuthUrl)
        {
            Content = new FormUrlEncodedContent(payload)
        };

        var response = await _httpClient.SendAsync(authRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (_logger.IsEnabled(LogLevel.Error))
            {
                LogPersonioAuthFailure(_logger, response.StatusCode, body);
            }
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!json.RootElement.TryGetProperty("data", out var data))
        {
            return null;
        }

        var token = data.TryGetProperty("token", out var tokenElement) ? tokenElement.GetString() : null;
        var expiresIn = data.TryGetProperty("expires_in", out var expiresElement) ? expiresElement.GetInt32() : 3600;

        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        _accessToken = token;
        _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, expiresIn - 60));
        return _accessToken;
    }
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Personio responded with {Status}: {Body}")]
    private static partial void LogPersonioResponseError(ILogger logger, System.Net.HttpStatusCode Status, string Body);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Personio auth failed: {Status} {Body}")]
    private static partial void LogPersonioAuthFailure(ILogger logger, System.Net.HttpStatusCode Status, string Body);
}
