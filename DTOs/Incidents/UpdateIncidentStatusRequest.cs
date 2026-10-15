using System.ComponentModel.DataAnnotations;
using NetSentinel.API.Models.Enums;

namespace NetSentinel.API.DTOs.Incidents;

public class UpdateIncidentStatusRequest
{
    [Required]
    public IncidentStatus Status { get; set; }
}