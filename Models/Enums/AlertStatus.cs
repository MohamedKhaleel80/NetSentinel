namespace NetSentinel.API.Models.Enums;

public enum AlertStatus
{
    New = 0,
    Acknowledged = 1,
    Investigating = 2,
    Resolved = 3,
    FalsePositive = 4
}