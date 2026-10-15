using System.ComponentModel.DataAnnotations;

namespace NetSentinel.API.DTOs.AuditLogs;

public class CreateAuditLogRequest
{
    public int? UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string EntityName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? EntityId { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }
}