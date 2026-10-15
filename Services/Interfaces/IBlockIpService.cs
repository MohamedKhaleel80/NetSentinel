using NetSentinel.API.DTOs.BlockedIps;

namespace NetSentinel.API.Services.Interfaces;

public interface IBlockIpService
{
    Task<BlockedIpResponse> BlockAsync(
        int incidentId,
        BlockIpRequest request,
        int? requestedByUserId);

    Task<BlockedIpResponse> UnblockAsync(
        int blockedIpId,
        int? requestedByUserId);

    Task<List<BlockedIpResponse>> GetAllAsync();

    Task<BlockedIpResponse?> GetByIdAsync(
        int id);
}