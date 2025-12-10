using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestEleven.PushServer.Services;
using RestEleven.Shared.Dtos;

namespace RestEleven.PushServer.Controllers;

[ApiController]
[Route("settings")]
[Authorize]
public class UserSettingsController : ControllerBase
{
    private readonly IUserSettingsStore _store;

    public UserSettingsController(IUserSettingsStore store)
    {
        _store = store;
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserSettingsDto>> GetAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var settings = await _store.GetAsync(userId, cancellationToken);
        if (settings is null)
        {
            return NotFound();
        }

        return Ok(settings);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpsertAsync([FromBody] UserSettingsDto payload, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await _store.SaveAsync(userId, payload, cancellationToken);
        return NoContent();
    }

    private string GetUserId()
    {
        return User.FindFirstValue("oid") ??
               User.FindFirstValue(ClaimTypes.NameIdentifier) ??
               throw new InvalidOperationException("Unable to determine user identifier from token.");
    }
}
