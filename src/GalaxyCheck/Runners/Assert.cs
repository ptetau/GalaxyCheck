using GalaxyCheck.Properties;
using GalaxyCheck.Runners;
using GalaxyCheck.Runners.Check;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GalaxyCheck
{
    public static partial class Extensions
    {
        /// <summary>Runs the property and throws <see cref="Runners.PropertyFailedException"/> when it fails.</summary>
        public static void Assert<T>(
            this IGen<Property.Test<T>> property,
            int? iterations = null,
            int? seed = null,
            int? size = null,
            int? shrinkLimit = null,
            string? replay = null,
            bool deepCheck = true,
            Func<string, string>? formatReproduction = null,
            Func<string, string>? formatMessage = null,
            TimeSpan? timeout = null,
            bool exhaustive = true,
            HealthCheck[]? suppressHealthChecks = null)
        {
            var checkResult = property.Check(iterations: iterations, seed: seed, size: size, shrinkLimit: shrinkLimit, replay: replay, deepCheck: deepCheck, timeout: timeout, exhaustive: exhaustive, suppressHealthChecks: suppressHealthChecks);
            ThrowIfFalsified(checkResult, formatReproduction, formatMessage);
        }

        /// <summary>Runs the async property and throws <see cref="Runners.PropertyFailedException"/> when it fails.</summary>
        public static async Task AssertAsync<T>(
            this IGen<Property.AsyncTest<T>> property,
            int? iterations = null,
            int? seed = null,
            int? size = null,
            int? shrinkLimit = null,
            string? replay = null,
            bool deepCheck = true,
            Func<string, string>? formatReproduction = null,
            Func<string, string>? formatMessage = null,
            TimeSpan? timeout = null,
            bool exhaustive = true,
            HealthCheck[]? suppressHealthChecks = null)
        {
            var checkResult = await property.CheckAsync(iterations: iterations, seed: seed, size: size, shrinkLimit: shrinkLimit, replay: replay, deepCheck: deepCheck, timeout: timeout, exhaustive: exhaustive, suppressHealthChecks: suppressHealthChecks);
            ThrowIfFalsified(checkResult, formatReproduction, formatMessage);
        }

        private static void ThrowIfFalsified<T>(
            CheckResult<T> checkResult,
            Func<string, string>? formatReproduction,
            Func<string, string>? formatMessage)
        {
            if (checkResult.Falsified)
            {
                throw new PropertyFailedException(
                    BoxCounterexample(checkResult.Counterexample!),
                    checkResult.Iterations,
                    checkResult.Shrinks,
                    formatReproduction,
                    formatMessage,
                    ReportBuilder.LabelLines(checkResult.LabelShares));
            }
        }

        private static Counterexample<object?> BoxCounterexample<T>(Counterexample<T> counterexample)
        {
            var value = (object)counterexample.Value!;
            return new Counterexample<object?>(
                value,
                counterexample.ReplayParameters,
                counterexample.Replay,
                counterexample.Exception,
                counterexample.PresentedInput);
        }
    }
}

namespace GalaxyCheck.Runners
{
    /// <summary>Thrown by Assert when a property fails. The message holds the counterexample, the replay string, and a JSON block for tools.</summary>
    public class PropertyFailedException : Exception
    {
        private readonly Counterexample<object?> _counterexample;

        /// <summary>Creates the exception for a counterexample.</summary>
        public PropertyFailedException(
            Counterexample<object?> counterexample,
            int iterations,
            int shrinks,
            Func<string, string>? formatReproduction,
            Func<string, string>? formatMessage)
            : this(counterexample, iterations, shrinks, formatReproduction, formatMessage, null)
        {
        }

        internal PropertyFailedException(
            Counterexample<object?> counterexample,
            int iterations,
            int shrinks,
            Func<string, string>? formatReproduction,
            Func<string, string>? formatMessage,
            IReadOnlyList<string>? labelLines)
            : base(FormatMessage(formatMessage, BuildMessage(counterexample, iterations, shrinks, formatReproduction, labelLines, withRecord: true)))
        {
            _counterexample = counterexample;
        }

        /// <summary>The stack trace of the exception of the counterexample, or else of this exception.</summary>
        public override string StackTrace => _counterexample.Exception?.StackTrace ?? base.StackTrace!;

        internal static string BuildReport(
            Counterexample<object?> counterexample,
            int iterations,
            int shrinks,
            IReadOnlyList<string>? labelLines) =>
                BuildMessage(counterexample, iterations, shrinks, null, labelLines, withRecord: false);

        private static string FormatMessage(Func<string, string>? formatMessage, string message) =>
            formatMessage == null
                ? message
                : formatMessage(message);

        /// <summary>The failure message: the readable lines, then the JSON block.</summary>
        /// <param name="withRecord">True to end the message with the JSON block of <see cref="FailureRecord"/>.</param>
        private static string BuildMessage(
            Counterexample<object?> counterexample,
            int iterations,
            int shrinks,
            Func<string, string>? formatReproduction,
            IReadOnlyList<string>? labelLines,
            bool withRecord)
        {
            var text = string.Join(Environment.NewLine, BuildLines(counterexample, iterations, shrinks, formatReproduction, labelLines));
            if (!withRecord)
            {
                return text;
            }

            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();
            if (AgentMode.IsOn && lines.Count > MaxAgentTextLines)
            {
                var hidden = lines.Count - (MaxAgentTextLines - 1);
                lines = lines.Take(MaxAgentTextLines - 1).ToList();
                lines.Add($"... ({hidden} more lines: the JSON block below holds the counterexample)");
            }

            lines.Add("");
            lines.AddRange(FailureRecord.Lines(counterexample, iterations, shrinks));
            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// The most lines of the readable part of the message in the quiet mode. The rest of the 40 lines is for the
        /// JSON block and for the text that a test framework adds.
        /// </summary>
        private const int MaxAgentTextLines = 28;

        private static IEnumerable<string> BuildLines(
            Counterexample<object?> counterexample,
            int iterations,
            int shrinks,
            Func<string, string>? formatReproduction,
            IReadOnlyList<string>? labelLines)
        {
            const string LineBreak = "";

            if (counterexample.Exception is CoverNotMetException coverNotMet)
            {
                yield return $"Falsified after {IterationsTrivia(iterations)}";
                yield return LineBreak;
                yield return $"---- {coverNotMet.Message}";
            }
            else
            {
                yield return FalsifiedAfterLine(iterations, shrinks);
                yield return ReproductionLine(counterexample, formatReproduction);
                yield return CounterexampleValueLine(counterexample);

                foreach (var line in CounterexampleAsCSharpLines(counterexample))
                {
                    yield return line;
                }

                yield return LineBreak;

                if (counterexample.Exception == null)
                {
                    yield return "Property function returned false";
                }
                else
                {
                    yield return ExceptionLine(counterexample.Exception);
                }
            }

            if (labelLines != null && labelLines.Count > 0)
            {
                yield return LineBreak;
                yield return "Labels:";
                foreach (var line in labelLines)
                {
                    yield return $"    {line}";
                }
            }
        }

        /// <summary>
        /// The section with the C# expression of each value. A value with no expression has the text of the
        /// counterexample line.
        /// </summary>
        private static IEnumerable<string> CounterexampleAsCSharpLines(Counterexample<object?> counterexample)
        {
            var values = counterexample.PresentedInput;
            if (values.Count == 0) return Array.Empty<string>();

            var lines = new List<string> { "Counterexample as C#:" };
            foreach (var value in values)
            {
                if (CSharpExpression.TryFormat(value, out var expression))
                {
                    lines.Add($"    {expression}");
                }
                else if (ExampleRenderer.HasNoReadableMembers(value))
                {
                    lines.Add("    (not available: the members of the value cannot be read)");
                }
                else
                {
                    lines.AddRange(ExampleRenderer.Render(new[] { value }).Select(l => $"    {l}"));
                }
            }

            return lines;
        }

        private static string FalsifiedAfterLine(int iterations, int shrinks) =>
            $"Falsified after {IterationsTrivia(iterations)} ({ShrinksTrivia(shrinks)})";

        private static string IterationsTrivia(int iterations) => iterations == 1 ? "1 test" : $"{iterations} tests";

        private static string ShrinksTrivia(int shrinks) => shrinks == 1 ? "1 shrink" : $"{shrinks} shrinks";

        private static string ReproductionLine(
            Counterexample<object?> counterexample,
            Func<string, string>? formatReproduction)
        {
            static string PrefixLine(string reproductionFormatted) => $"Reproduction: {reproductionFormatted}";

            if (formatReproduction == null)
            {
                return PrefixLine(counterexample.Replay);
            }
            else
            {
                return PrefixLine(formatReproduction(counterexample.Replay));
            }
        }

        private static string CounterexampleValueLine(Counterexample<object?> counterexample)
        {
            var lines = ExampleRenderer.Render(counterexample.PresentedInput).ToList();

            return lines.Count switch
            {
                0 => throw new Exception("Fatal: Expected at least one line to be rendered in counterexample"),
                1 => $"Counterexample: {lines.Single()}",
                _ => string.Join(
                    Environment.NewLine,
                    Enumerable.Concat(
                        new[] { "Counterexample:" },
                        lines.Select(l => $"    {l}")))
            };
        }

        /// <summary>
        /// The message of the exception of the counterexample. The message can hold the JSON block of another failure,
        /// for example of a property that the test asserts inside. Its marker lines get other names, so that the
        /// message holds only the block of this failure.
        /// </summary>
        private static string ExceptionLine(Exception ex)
        {
            var lines = $"---- {ex.Message}".Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var inInnerBlock = false;
            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].Trim();
                if (trimmed == FailureRecord.StartLine)
                {
                    lines[i] = "    [inner failure, not of this test]";
                    inInnerBlock = true;
                }
                else if (trimmed == FailureRecord.EndLine)
                {
                    lines[i] = "    [end of inner failure]";
                    inInnerBlock = false;
                }
                else if (inInnerBlock)
                {
                    lines[i] = "    " + lines[i];
                }
            }

            return string.Join(Environment.NewLine, lines);
        }
    }
}
