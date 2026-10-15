using System.ComponentModel.DataAnnotations;

namespace NetSentinel.API.DTOs.Notifications;

public class RegisterFcmTokenRequest
{
    [Required]
    [MaxLength(1000)]
    public string Token { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? DeviceIdentifier { get; set; }

    [MaxLength(50)]
    public string? Platform { get; set; }
}