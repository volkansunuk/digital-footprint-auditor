using DigitalFootprintAuditor.Application.Realtime;

namespace DigitalFootprintAuditor.Application.Abstractions;

public interface IScanProgressNotifier
{
    Task NotifyAsync(
        ScannerProgressDto progress,
        CancellationToken cancellationToken);
}