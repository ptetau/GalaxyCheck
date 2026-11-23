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
                await Assert(
                    parameters,
                    replay: null,
                    onReplay: replay => newReplay = replay,
                    formatMessage: message => FormatMessage(message) + FailureMemoryNote(parameters));
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
                // The saved failure still falsifies the property: it fails the test, and the entry stays.
                throw;
            }
            catch (Exception ex)
            {
                // The saved replay is stale: it does not decode, no longer fits the generators, discards, or hits a
                // generator error. Drop the entry and run the property as normal. A real generator error throws
                // again in the generated run, with its own message. A shared key loses its entry only when the
                // replay does not decode, as the replay can fit the row or fixture that saved it.
                if (!savedFailure.IsShared || IsDecodeError(ex))
                {
                    TryChange(savedFailure.Remove);
                }

                return;
            }

            // The saved replay passes. A shared key keeps its entry, as the replay can be the failure of another row
            // or fixture of the test.
            if (!savedFailure.IsShared)
            {
                TryChange(savedFailure.Remove);
            }
        }

        private static bool IsDecodeError(Exception? ex)
        {
            for (; ex != null; ex = ex.InnerException)
            {
                if (ex is Exceptions.GenErrorException &&
                    ex.Message.StartsWith("Error decoding replay string", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static Task Assert(
            PropertyRunParameters parameters,
            string? replay,
            Action<string>? onReplay,
            Func<string, string>? formatMessage = null) =>
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
                formatMessage: formatMessage ?? FormatMessage);

        /// <summary>The note, one time for each assembly, that the failure memory has no folder to save to.</summary>
        private static string FailureMemoryNote(PropertyRunParameters parameters) =>
            parameters.TakeFailureMemoryNote?.Invoke() is { } note
                ? Environment.NewLine + Environment.NewLine + note
                : "";

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
}
