namespace NetSentinel.API.DTOs.Devices;

public class DeviceScanResponse
{
    public string Subnet { get; set; } = string.Empty;

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset CompletedAt { get; set; }

    public int DiscoveredCount { get; set; }

    public int AddedCount { get; set; }

    public int UpdatedCount { get; set; }

    public List<DeviceResponse> Devices { get; set; } = [];
}