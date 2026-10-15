using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetSentinel.API.DTOs.Incidents;
using NetSentinel.API.Services.Interfaces;

namespace NetSentinel.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IncidentsController : ControllerBase
{
    private readonly IIncidentService _incidentService;

    public IncidentsController(
        IIncidentService incidentService)
    {
        _incidentService = incidentService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,SecurityAnalyst")]
    public async Task<ActionResult<IncidentResponse>> Create(
        [FromBody] CreateIncidentRequest request)
    {
        try
        {
            var result =
                await _incidentService.CreateAsync(
                    request,
                    GetCurrentUserId());

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Id },
                result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<IncidentResponse>>>
        GetAll()
    {
        var result =
            await _incidentService.GetAllAsync();

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<IncidentResponse>>
        GetById(int id)
    {
        var result =
            await _incidentService.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound(new
            {
                message = "Incident not found."
            });
        }

        return Ok(result);
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Admin,SecurityAnalyst")]
    public async Task<ActionResult<IncidentResponse>>
        UpdateStatus(
            int id,
            [FromBody] UpdateIncidentStatusRequest request)
    {
        try
        {
            var result =
                await _incidentService.UpdateStatusAsync(
                    id,
                    request,
                    GetCurrentUserId());

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpPut("{id:int}/assign")]
    [Authorize(Roles = "Admin,SecurityAnalyst")]
    public async Task<ActionResult<IncidentResponse>>
        Assign(
            int id,
            [FromBody] AssignIncidentRequest request)
    {
        try
        {
            var result =
                await _incidentService.AssignAsync(
                    id,
                    request,
                    GetCurrentUserId());

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (int.TryParse(
            userIdClaim,
            out var userId))
        {
            return userId;
        }

        return null;
    }
}