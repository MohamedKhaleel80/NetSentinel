using System.ComponentModel.DataAnnotations;

namespace NetSentinel.API.DTOs.Incidents;

public class AssignIncidentRequest
{
    [Required]
    public int UserId { get; set; }
}