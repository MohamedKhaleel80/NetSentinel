namespace NetSentinel.API.DTOs.Dashboard;

public class DashboardResponse
{
    public int TotalAlerts { get; set; }
    public int CriticalAlerts { get; set; }
    public int HighAlerts { get; set; }
    public int MediumAlerts { get; set; }
    public int LowAlerts { get; set; }

    public int OpenIncidents { get; set; }
    public int InProgressIncidents { get; set; }
    public int ContainedIncidents { get; set; }
    public int ResolvedIncidents { get; set; }

    public int BlockedIps { get; set; }
    public int OnlineDevices { get; set; }
    public int OfflineDevices { get; set; }

    public int AlertsToday { get; set; }
    public int AlertsLast24Hours { get; set; }

    public string MonitoringStatus { get; set; } = "No Recent Events";

    public DateTimeOffset? LastEventReceivedAt { get; set; }

    public List<TopIpResponse> TopAttackingIps { get; set; } = new();

    public List<TopRuleResponse> TopTriggeredRules { get; set; } = new();
}

public class TopIpResponse
{
    public string IpAddress { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TopRuleResponse
{
    public string Rule { get; set; } = string.Empty;
    public int Count { get; set; }
}