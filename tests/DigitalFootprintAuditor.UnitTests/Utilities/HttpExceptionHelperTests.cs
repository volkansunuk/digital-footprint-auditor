using DigitalFootprintAuditor.Infrastructure.Utilities;

namespace DigitalFootprintAuditor.UnitTests.Utilities;

public class HttpExceptionHelperTests
{
    [Fact]
    public void IsTimeout_TaskCanceledWithoutUserCancellation_ReturnsTrue()
    {
        var isTimeout = HttpExceptionHelper.IsTimeout(
            new TaskCanceledException(),
            CancellationToken.None);

        Assert.True(isTimeout);
    }

    [Fact]
    public void IsTimeout_UserCancellationRequested_ReturnsFalse()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var isTimeout = HttpExceptionHelper.IsTimeout(
            new TaskCanceledException(),
            cancellationSource.Token);

        Assert.False(isTimeout);
    }
}
