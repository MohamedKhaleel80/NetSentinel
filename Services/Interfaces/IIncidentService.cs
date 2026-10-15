using NetSentinel.API.DTOs.Incidents;

namespace NetSentinel.API.Services.Interfaces;

public interface IIncidentService
{
    Task<IncidentResponse> CreateAsync(
        CreateIncidentRequest request,
        int? userId);

    Task<List<IncidentResponse>> GetAllAsync();

    Task<IncidentResponse?> GetByIdAsync(
        int id);

    Task<IncidentResponse> UpdateStatusAsync(
        int id,
        UpdateIncidentStatusRequest request,
        int? userId);

    Task<IncidentResponse> AssignAsync(
        int id,
        AssignIncidentRequest request,
        int? userId);
}