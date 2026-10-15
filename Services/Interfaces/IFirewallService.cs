namespace NetSentinel.API.Services.Interfaces;

public interface IFirewallService
{
    Task<bool> BlockIpAsync(string ipAddress);

    Task<bool> UnblockIpAsync(string ipAddress);
}