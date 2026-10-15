using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NetSentinel.API.DTOs.Snort;
using NetSentinel.API.Models.Settings;
using NetSentinel.API.Services.Interfaces;

namespace NetSentinel.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SnortController : ControllerBase
{
    private readonly ISnortService _snortService;
    private readonly SnortSettings _snortSettings;
    private readonly ILogger<SnortController> _logger;

    public SnortController(
        ISnortService snortService,
        IOptions<SnortSettings> snortOptions,
        ILogger<SnortController> logger)
    {
        _snortService = snortService;
        _snortSettings = snortOptions.Value;
        _logger = logger;
    }

    [HttpPost("alert")]
    public async Task<IActionResult> ReceiveAlert(
        [FromBody] SnortAlertDto request)
    {
        if (string.IsNullOrWhiteSpace(
            _snortSettings.ApiKey))
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message =
                        "Snort API key is not configured."
                });
        }

        if (!Request.Headers.TryGetValue(
            "X-Snort-Api-Key",
            out var providedApiKey))
        {
            return Unauthorized(new
            {
                message =
                    "Snort API key is missing."
            });
        }

        var providedKeyBytes =
            Encoding.UTF8.GetBytes(providedApiKey.ToString());

        var configuredKeyBytes =
            Encoding.UTF8.GetBytes(_snortSettings.ApiKey);

        if (!CryptographicOperations.FixedTimeEquals(
            providedKeyBytes,
            configuredKeyBytes))
        {
            return Unauthorized(new
            {
                message =
                    "Invalid Snort API key."
            });
        }

        try
        {
            var alertId =
                await _snortService.ProcessAlertAsync(
                    request);

            return Ok(new
            {
                message =
                    "Snort alert processed successfully.",

                alertId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process an authenticated Snort alert.");

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
            {
                message = "Snort alert processing failed."
            });
        }
    }
}