using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Application.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace DigitalFootprintAuditor.Api.Hubs;

public class SignalRScanProgressNotifier : IScanProgressNotifier
{
    private readonly IHubContext<ScanProgressHub> _hubContext;

    public SignalRScanProgressNotifier(
        IHubContext<ScanProgressHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyAsync(
        ScannerProgressDto progress,
        CancellationToken cancellationToken)
    {
        return _hubContext.Clients
            .Group(progress.ScanId.ToString())
            .SendAsync(
                "ScannerProgressUpdated",
                progress,
                cancellationToken);
    }
}