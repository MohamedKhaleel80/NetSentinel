using System.ComponentModel.DataAnnotations;
using NetSentinel.API.Models.Enums;

namespace NetSentinel.API.DTOs.Alerts;

public class UpdateAlertStatusRequest
{
    [Required]
    public AlertStatus Status { get; set; }
}
