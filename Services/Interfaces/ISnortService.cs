using NetSentinel.API.DTOs.Snort;

namespace NetSentinel.API.Services.Interfaces;

public interface ISnortService
{
    Task<long> ProcessAlertAsync(
        SnortAlertDto snortAlert);
}