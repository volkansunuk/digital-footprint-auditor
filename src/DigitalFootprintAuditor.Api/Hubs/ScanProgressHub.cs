using Microsoft.AspNetCore.SignalR;

namespace DigitalFootprintAuditor.Api.Hubs;

public class ScanProgressHub : Hub
{
    public async Task JoinScanGroup(Guid scanId)
    {
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            scanId.ToString());
    }

    public async Task LeaveScanGroup(Guid scanId)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            scanId.ToString());
    }
}