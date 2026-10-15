using System.ComponentModel.DataAnnotations;

namespace NetSentinel.API.DTOs.BlockedIps;

public class BlockIpRequest
{
    [MaxLength(500)]
    public string? Reason { get; set; }
}