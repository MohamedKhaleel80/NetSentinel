namespace NetSentinel.API.DTOs.Incidents;

public class IncidentResponse
{
    public int Id { get; set; }

    public long AlertId { get; set; }

    public string SourceIp { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? AssignedToUserId { get; set; }

    public string? AssignedToUserName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }
}