namespace DigitalFootprintAuditor.Infrastructure.Utilities;

public static class HttpExceptionHelper
{
    public static bool IsTimeout(Exception ex, CancellationToken userCancellationToken)
    {
        return ex is TaskCanceledException && !userCancellationToken.IsCancellationRequested;
    }
}