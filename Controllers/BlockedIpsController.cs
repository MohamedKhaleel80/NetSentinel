using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetSentinel.API.DTOs.BlockedIps;
using NetSentinel.API.Services.Interfaces;

namespace NetSentinel.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BlockedIpsController : ControllerBase
{
    private readonly IBlockIpService _blockIpService;

    public BlockedIpsController(
        IBlockIpService blockIpService)
    {
        _blockIpService = blockIpService;
    }

    [HttpPost("incidents/{incidentId:int}")]
    [Authorize(Roles = "Admin,SecurityAnalyst")]
    public async Task<ActionResult<BlockedIpResponse>> Block(
        int incidentId,
        [FromBody] BlockIpRequest request)
    {
        try
        {
            var result =
                await _blockIpService.BlockAsync(
                    incidentId,
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

    [HttpPost("{id:int}/unblock")]
    [Authorize(Roles = "Admin,SecurityAnalyst")]
    public async Task<ActionResult<BlockedIpResponse>> Unblock(
        int id)
    {
        try
        {
            var result =
                await _blockIpService.UnblockAsync(
                    id,
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

    [HttpGet]
    public async Task<ActionResult<List<BlockedIpResponse>>>
        GetAll()
    {
        var result =
            await _blockIpService.GetAllAsync();

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BlockedIpResponse>>
        GetById(int id)
    {
        var result =
            await _blockIpService.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound(new
            {
                message = "Blocked IP record not found."
            });
        }

        return Ok(result);
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