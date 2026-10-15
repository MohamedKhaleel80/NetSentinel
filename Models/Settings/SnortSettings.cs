namespace NetSentinel.API.Models.Settings;

public class SnortSettings
{
    public bool Enabled { get; set; }

    public string AlertFilePath { get; set; }
        = "alert_json.txt";

    public int PollIntervalMilliseconds { get; set; }
        = 500;

    public string ApiKey { get; set; }
        = string.Empty;
}