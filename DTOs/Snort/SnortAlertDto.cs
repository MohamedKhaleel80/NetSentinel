using System.Text.Json.Serialization;
using System.Text.Json;

namespace NetSentinel.API.DTOs.Snort;

public class SnortAlertDto
{
    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("pkt_num")]
    public long PacketNumber { get; set; }

    [JsonPropertyName("proto")]
    public string? Protocol { get; set; }

    [JsonPropertyName("pkt_gen")]
    public string? PacketGenerator { get; set; }

    [JsonPropertyName("pkt_len")]
    public int PacketLength { get; set; }

    [JsonPropertyName("dir")]
    public string? Direction { get; set; }

    [JsonPropertyName("src_addr")]
    public string? SourceAddress { get; set; }

    [JsonPropertyName("src_ap")]
    public string? LegacySourceAddress { get; set; }

    [JsonPropertyName("dst_addr")]
    public string? DestinationAddress { get; set; }

    [JsonPropertyName("dst_ap")]
    public string? LegacyDestinationAddress { get; set; }

    [JsonPropertyName("rule")]
    public string? Rule { get; set; }

    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("msg")]
    public string? Message { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}