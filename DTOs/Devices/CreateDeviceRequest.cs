using System.ComponentModel.DataAnnotations;

namespace NetSentinel.API.DTOs.Devices;

public class CreateDeviceRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(45)]
    public string IpAddress { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? MacAddress { get; set; }

    [MaxLength(255)]
    public string? Hostname { get; set; }

    [MaxLength(100)]
    public string? OperatingSystem { get; set; }

    [MaxLength(50)]
    public string? DeviceType { get; set; }
}