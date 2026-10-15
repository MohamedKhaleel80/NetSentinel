using NetSentinel.API.Models.Enums;

namespace NetSentinel.API.Models.Entities;

public class Device
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string IpAddress { get; set; } = string.Empty;

    public string? MacAddress { get; set; }

    public string? Hostname { get; set; }

    public string? OperatingSystem { get; set; }

    public string? DeviceType { get; set; }

    public DeviceStatus Status { get; set; } = DeviceStatus.Unknown;

    public DateTimeOffset? LastSeen { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<Alert> Alerts { get; set; }
        = new List<Alert>();
}