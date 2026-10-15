using NetSentinel.API.DTOs.Dashboard;

namespace NetSentinel.API.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardResponse> GetDashboardAsync();
}