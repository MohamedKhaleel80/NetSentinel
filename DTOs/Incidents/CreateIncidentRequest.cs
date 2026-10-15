using System.ComponentModel.DataAnnotations;

namespace NetSentinel.API.DTOs.Incidents;

public class CreateIncidentRequest
{
    [Required]
    public long AlertId { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }
}