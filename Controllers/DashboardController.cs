using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetSentinel.API.DTOs.Dashboard;
using NetSentinel.API.Services.Interfaces;

namespace NetSentinel.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(
        IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardResponse>>
        GetDashboard()
    {
        var result =
            await _dashboardService.GetDashboardAsync();

        return Ok(result);
    }
}