using NetSentinel.API.Models.Enums;

namespace NetSentinel.API.DTOs.Devices;

public class DeviceResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? MacAddress { get; set; }
    public string? Hostname { get; set; }
    public string? OperatingSystem { get; set; }
    public string? DeviceType { get; set; }
    public DeviceStatus Status { get; set; }
    public DateTimeOffset? LastSeen { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}