using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RestEleven.PersonioBridge.Services;
using RestEleven.Shared.Dtos;

namespace RestEleven.PersonioBridge.Controllers;

[ApiController]
[Route("personio")]
[Authorize]
public partial class AttendanceController : ControllerBase
{
    private readonly IPersonioClient _client;
    private readonly ILogger<AttendanceController> _logger;

    public AttendanceController(IPersonioClient client, ILogger<AttendanceController> logger)
    {
        _client = client;
        _logger = logger;
    }

    [HttpPost("attendance")]
    public async Task<IActionResult> CreateAttendance([FromBody] CreateAttendanceDto dto, CancellationToken cancellationToken)
    {
        var result = await _client.CreateAttendanceAsync(dto, cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            var detail = result.Errors.Count > 0 ? string.Join("; ", result.Errors) : "Unbekannter Fehler";
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                LogPersonioBridgeError(_logger, detail);
            }
            return Problem(detail: detail, statusCode: StatusCodes.Status502BadGateway);
        }

        return Ok(result.Value);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "PersonioBridge error: {Detail}")]
    private static partial void LogPersonioBridgeError(ILogger logger, string Detail);
}
