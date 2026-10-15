using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetSentinel.API.DTOs.Alerts;
using NetSentinel.API.DTOs.Common;
using NetSentinel.API.Services.Interfaces;

namespace NetSentinel.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AlertsController : ControllerBase
{
    private readonly IAlertService _alertService;

    public AlertsController(
        IAlertService alertService)
    {
        _alertService = alertService;
    }

    [HttpGet]
    public async Task<
        ActionResult<PagedResult<AlertResponse>>>
        GetAlerts(
            [FromQuery] AlertQueryRequest request)
    {
        var result =
            await _alertService.GetAlertsAsync(request);

        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<AlertResponse>>
        GetAlert(long id)
    {
        var alert =
            await _alertService
                .GetAlertByIdAsync(id);

        if (alert == null)
        {
            return NotFound(new
            {
                message = "Alert not found."
            });
        }

        return Ok(alert);
    }

    [HttpPut("{id:long}/status")]
    [HttpPatch("{id:long}/status")]
    [Authorize(Roles = "Admin,SecurityAnalyst")]
    public async Task<ActionResult<AlertResponse>> UpdateStatus(
        long id,
        [FromBody] UpdateAlertStatusRequest request)
    {
        var userId = GetCurrentUserId();
        var alert = await _alertService.UpdateStatusAsync(id, request.Status, userId);

        if (alert == null)
        {
            return NotFound(new
            {
                message = "Alert not found."
            });
        }

        return Ok(alert);
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}