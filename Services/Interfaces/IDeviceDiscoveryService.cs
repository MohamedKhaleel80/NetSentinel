using NetSentinel.API.DTOs.Devices;

namespace NetSentinel.API.Services.Interfaces;

public interface IDeviceDiscoveryService
{
    Task<DeviceScanResponse> ScanAsync(
        int? userId,
        CancellationToken cancellationToken = default);
}