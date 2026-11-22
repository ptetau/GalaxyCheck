using GalaxyCheck;

namespace XunitTimeout;

public class TimeoutTests
{
    [Property(Iterations = 5, TimeoutMilliseconds = 100)]
    public async Task<bool> OverTheLimit(int x)
    {
        await Task.Delay(5000);
        return true;
    }

    [Property(Iterations = 5)]
    public async Task<bool> WithTokenParameter(int x, CancellationToken cancellationToken)
    {
        await Task.Yield();
        return false;
    }
}
