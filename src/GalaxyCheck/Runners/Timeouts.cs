using GalaxyCheck.Properties;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GalaxyCheck.Runners
{
    /// <summary>
    /// Runs the test function of one test case under a time limit. The function runs on its own thread, so that a
    /// function that never returns cannot hold the run. The runner leaves such a thread behind: it does not abort it.
    /// </summary>
    [global::Verifiable.Contracts.Unverifiable]
    internal static class Timeouts
    {
        /// <summary>The time that a function gets to end after its token is cancelled.</summary>
        private static readonly TimeSpan CancellationGrace = TimeSpan.FromMilliseconds(250);

        public static Property.TestOutput? RunSync(Lazy<Property.TestOutput> output, long milliseconds, out Exception? error)
        {
            var task = Task.Factory.StartNew(
                () => output.Value,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

            bool completed;
            try
            {
                completed = task.Wait(TimeSpan.FromMilliseconds(milliseconds));
            }
            catch (AggregateException ex)
            {
                error = ex.InnerException ?? ex;
                return null;
            }

            if (!completed)
            {
                Observe(task);
                error = new TestTimeoutException(milliseconds);
                return null;
            }

            error = null;
            return task.Result;
        }

        public static async Task<(Property.TestOutput?, Exception?)> RunAsync(
            Lazy<ValueTask<Property.TestOutput>> output,
            long milliseconds)
        {
            var cancellation = new CancellationTokenSource();
            var token = cancellation.Token;

            var task = Task.Factory.StartNew(
                () =>
                {
                    TestContext.Token = token;
                    try
                    {
                        return output.Value.AsTask();
                    }
                    finally
                    {
                        TestContext.Token = default;
                    }
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).Unwrap();

            try
            {
                return (await task.WaitAsync(TimeSpan.FromMilliseconds(milliseconds)).ConfigureAwait(false), null);
            }
            catch (Exception ex)
            {
                if (task.IsCompleted)
                {
                    return (null, ex);
                }
            }

            cancellation.Cancel();
            try
            {
                await task.WaitAsync(CancellationGrace).ConfigureAwait(false);
            }
            catch
            {
                // The function ended with an error or did not end. The test case is over the limit either way.
            }

            Observe(task);
            return (null, new TestTimeoutException(milliseconds));
        }

        private static void Observe(Task task)
        {
            task.ContinueWith(
                finished => _ = finished.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }
    }
}
