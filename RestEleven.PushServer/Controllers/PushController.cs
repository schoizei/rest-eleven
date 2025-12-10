using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RestEleven.PushServer.Services;
using RestEleven.Shared.Dtos;
using RestEleven.Shared.Results;

namespace RestEleven.PushServer.Controllers;

[ApiController]
[Route("push")]
[Authorize]
public partial class PushController : ControllerBase
{
    private readonly IPushSubscriptionStore _store;
    private readonly IWebPushService _pushService;
    private readonly ILogger<PushController> _logger;

    public PushController(IPushSubscriptionStore store, IWebPushService pushService, ILogger<PushController> logger)
    {
        _store = store;
        _pushService = pushService;
        _logger = logger;
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] SubscriptionDto dto, CancellationToken cancellationToken)
    {
        await _store.UpsertAsync(dto, cancellationToken);
        return Ok();
    }

    [HttpPost("notify")]
    public async Task<IActionResult> Notify([FromBody] NotificationDto dto, CancellationToken cancellationToken)
    {
        var result = await _pushService.BroadcastAsync(dto, cancellationToken);
        if (!result.Succeeded)
        {
            return Problem(detail: string.Join("; ", result.Errors), statusCode: StatusCodes.Status500InternalServerError);
        }

        return Ok(new { delivered = result.Value });
    }

    [HttpPost("demo")]
    public async Task<IActionResult> Demo(CancellationToken cancellationToken)
    {
        var payload = new NotificationDto
        {
            Title = "RestEleven Demo",
            Body = "Dies ist eine Testbenachrichtigung.",
            Url = "/"
        };

        var result = await _pushService.BroadcastAsync(payload, cancellationToken);
        if (!result.Succeeded)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                foreach (var error in result.Errors)
                {
                    LogDemoNotificationFailed(_logger, error);
                }
            }
        }

        return Accepted(new { delivered = result.Value });
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Demo notification failed: {Error}")]
    private static partial void LogDemoNotificationFailed(ILogger logger, string Error);
}
