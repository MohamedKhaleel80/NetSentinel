using NetSentinel.API.DTOs.Devices;

namespace NetSentinel.API.Services.Interfaces;

public interface IDeviceService
{
    Task<DeviceResponse> CreateAsync(
        CreateDeviceRequest request,
        int? userId);

    Task<List<DeviceResponse>> GetAllAsync();

    Task<DeviceResponse?> GetByIdAsync(int id);

    Task<DeviceResponse> UpdateAsync(
        int id,
        UpdateDeviceRequest request,
        int? userId);

    Task DeleteAsync(
        int id,
        int? userId);
}