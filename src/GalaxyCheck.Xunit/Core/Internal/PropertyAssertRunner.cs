using GalaxyCheck.Runners;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GalaxyCheck.Internal
{
    /// <summary>
    /// Runs a property in the order of a PropertyRun: a [Replay] or a saved failure first, then each [Example], then
    /// the generated test cases. A falsified run saves its replay string in the failure memory.
    /// </summary>
    internal class PropertyAssertRunner : IPropertyRunner
    {
        public async Task Run(PropertyRunParameters parameters)
        {
            if (parameters.Replay != null)
            {
                await Assert(parameters, parameters.Replay, onReplay: null);
                return;
            }

            var savedFailure = parameters.SavedFailure;
            if (savedFailure != null)
            {
                await ReplaySavedFailure(parameters, savedFailure);
            }

            foreach (var example in parameters.Examples ?? Array.Empty<PropertyExample>())
            {
                await PropertyExamples.Run(example, FormatMessage);
            }

            string? newReplay = null;
            try
            {
                await Assert(parameters, replay: null, onReplay: replay => newReplay = replay);
            }
            catch (PropertyFailedException)
            {
                if (savedFailure != null && newReplay != null)
                {
                    TryChange(() => savedFailure.Save(newReplay));
                }

                throw;
            }
        }

        private static async Task ReplaySavedFailure(PropertyRunParameters parameters, SavedFailure savedFailure)
        {
            string? savedReplay = null;
            TryChange(() => savedReplay = savedFailure.Load());
            if (savedReplay == null)
            {
                return;
            }

            try
            {
                await Assert(parameters, savedReplay, onReplay: null);
            }
            catch (PropertyFailedException)
            {
                // The saved failure still fails: keep it, and report it.
                throw;
            }
            catch (Exception ex)
            {
                TryChange(savedFailure.Remove);
                throw new SavedFailureException(savedFailure, ex);
            }

            TryChange(savedFailure.Remove);
        }

        private static Task Assert(PropertyRunParameters parameters, string? replay, Action<string>? onReplay) =>
            parameters.Property.AssertAsync(
                replay: replay,
                seed: replay == null ? parameters.Seed : null,
                size: replay == null ? parameters.Size : null,
                iterations: parameters.Iterations,
                shrinkLimit: parameters.ShrinkLimit,
                timeout: parameters.Timeout,
                exhaustive: parameters.Exhaustive,
                suppressHealthChecks: parameters.SuppressHealthChecks,
                formatReproduction: (newReplay) =>
                {
                    onReplay?.Invoke(newReplay);
                    return $"{Environment.NewLine}    [Replay(\"{newReplay}\")]";
                },
                formatMessage: FormatMessage);

        private static string FormatMessage(string message) => Environment.NewLine + Environment.NewLine + message;

        /// <summary>The failure memory is a help: a file that another process holds does not fail the test.</summary>
        private static void TryChange(Action change)
        {
            try
            {
                change();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
            }
        }
    }

    /// <summary>Thrown when the saved replay string of a test does not run, for example when it does not decode.</summary>
    internal sealed class SavedFailureException : Exception
    {
        public SavedFailureException(SavedFailure savedFailure, Exception inner)
            : base(
                $"The saved failure of '{savedFailure.TestName}' in {FailureFile.FolderName}/{FailureFile.FileName} did not run: {inner.Message}" +
                Environment.NewLine +
                "Fix: the entry is now removed; run the test again.",
                inner)
        {
        }
    }
}
