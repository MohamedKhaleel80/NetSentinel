using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetSentinel.API.DTOs.AuditLogs;
using NetSentinel.API.Services.Interfaces;

namespace NetSentinel.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,SecurityAnalyst")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(
        IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<ActionResult<List<AuditLogResponse>>>
        GetAll()
    {
        var result =
            await _auditLogService.GetAllAsync();

        return Ok(result);
    }

    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<List<AuditLogResponse>>>
        GetByUserId(int userId)
    {
        var result =
            await _auditLogService
                .GetByUserIdAsync(userId);

        return Ok(result);
    }
}