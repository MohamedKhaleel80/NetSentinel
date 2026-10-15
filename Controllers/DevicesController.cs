
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetSentinel.API.DTOs.Devices;
using NetSentinel.API.Services.Interfaces;

namespace NetSentinel.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DevicesController : ControllerBase
{
    private readonly IDeviceService _deviceService;
    private readonly IDeviceDiscoveryService _discoveryService;

    public DevicesController(
        IDeviceService deviceService,
        IDeviceDiscoveryService discoveryService)
    {
        _deviceService = deviceService;
        _discoveryService = discoveryService;
    }

    // POST: api/Devices
    [HttpPost]
    [Authorize(Roles = "Admin,SecurityAnalyst")]
    public async Task<ActionResult<DeviceResponse>> Create(
        [FromBody] CreateDeviceRequest request)
    {
        var userId = GetCurrentUserId();

        var device = await _deviceService.CreateAsync(
            request,
            userId);

        return CreatedAtAction(
            nameof(GetById),
            new { id = device.Id },
            device);
    }

    // GET: api/Devices
    [HttpGet]
    public async Task<ActionResult<List<DeviceResponse>>> GetAll()
    {
        var devices = await _deviceService.GetAllAsync();

        return Ok(devices);
    }

    // GET: api/Devices/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<DeviceResponse>> GetById(int id)
    {
        var device = await _deviceService.GetByIdAsync(id);

        if (device is null)
        {
            return NotFound(new
            {
                message = $"Device with ID {id} was not found."
            });
        }

        return Ok(device);
    }

    // PUT: api/Devices/5
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,SecurityAnalyst")]
    public async Task<ActionResult<DeviceResponse>> Update(
        int id,
        [FromBody] UpdateDeviceRequest request)
    {
        var userId = GetCurrentUserId();

        var device = await _deviceService.UpdateAsync(
            id,
            request,
            userId);

        if (device is null)
        {
            return NotFound(new
            {
                message = $"Device with ID {id} was not found."
            });
        }

        return Ok(device);
    }

    // DELETE: api/Devices/5
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var existingDevice = await _deviceService.GetByIdAsync(id);

        if (existingDevice is null)
        {
            return NotFound(new
            {
                message = $"Device with ID {id} was not found."
            });
        }

        await _deviceService.DeleteAsync(id, GetCurrentUserId());

        return NoContent();
    }


    // POST: api/Devices/scan
    [HttpPost("scan")]
    [Authorize(Roles = "Admin,SecurityAnalyst")]
    public async Task<ActionResult<DeviceScanResponse>> Scan(
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _discoveryService.ScanAsync(
                GetCurrentUserId(),
                cancellationToken);

            return Ok(result);
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
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub");

        return int.TryParse(userId, out var id)
            ? id
            : null;
    }
}
