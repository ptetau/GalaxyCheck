using GalaxyCheck.Properties;
using GalaxyCheck.Runners;
using GalaxyCheck.Runners.Check;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            if (checkResult.CoverageFailure != null)
            {
                throw new PropertyFailedException(
                    checkResult.CoverageFailure,
                    checkResult.Iterations,
                    checkResult.InitialParameters.Seed,
                    formatMessage,
                    ReportBuilder.LabelLines(checkResult.LabelShares));
            }

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
    [global::Verifiable.Contracts.Unverifiable]
    public class PropertyFailedException : Exception
    {
        private readonly Counterexample<object?>? _counterexample;

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

        /// <summary>
        /// Creates the exception for a cover rule that the run did not meet. The message states the label, the
        /// required share, and the share that was reached, and holds no counterexample and no replay string.
        /// </summary>
        internal PropertyFailedException(
            CoverageFailure coverage,
            int iterations,
            int seed,
            Func<string, string>? formatMessage,
            IReadOnlyList<string>? labelLines)
            : base(FormatMessage(formatMessage, BuildCoverageMessage(coverage, iterations, seed, labelLines, withRecord: true)))
        {
        }

        /// <summary>The stack trace of the exception of the counterexample, or else of this exception.</summary>
        public override string StackTrace => _counterexample?.Exception?.StackTrace ?? base.StackTrace!;

        internal static string BuildReport(
            Counterexample<object?> counterexample,
            int iterations,
            int shrinks,
            IReadOnlyList<string>? labelLines) =>
                BuildMessage(counterexample, iterations, shrinks, null, labelLines, withRecord: false);

        internal static string BuildCoverageReport(
            CoverageFailure coverage,
            int iterations,
            IReadOnlyList<string>? labelLines) =>
                BuildCoverageMessage(coverage, iterations, 0, labelLines, withRecord: false);

        private static string BuildCoverageMessage(
            CoverageFailure coverage,
            int iterations,
            int seed,
            IReadOnlyList<string>? labelLines,
            bool withRecord)
        {
            var lines = new List<string>
            {
                $"Falsified after {IterationsTrivia(iterations)}",
                "",
                $"---- {coverage.Message}",
            };
            lines.AddRange(LabelSection(labelLines));

            var text = RenameMarkerLines(string.Join(Environment.NewLine, lines));
            if (!withRecord)
            {
                return text;
            }

            return string.Join(
                Environment.NewLine,
                new[] { text, "" }.Concat(FailureRecord.CoverageLines(coverage, iterations, seed)));
        }

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
            var text = RenameMarkerLines(
                string.Join(Environment.NewLine, BuildLines(counterexample, iterations, shrinks, formatReproduction, labelLines)));
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

            foreach (var line in LabelSection(labelLines))
            {
                yield return line;
            }
        }

        private static IEnumerable<string> LabelSection(IReadOnlyList<string>? labelLines)
        {
            if (labelLines == null || labelLines.Count == 0) yield break;

            yield return "";
            yield return "Labels:";
            foreach (var line in labelLines)
            {
                yield return $"    {line}";
            }
        }

        /// <summary>
        /// The section with the C# expression of each value. The section holds only valid C#: when a value has no
        /// expression, for example the commands of a stateful test or a tuple that holds a function, the message has
        /// no section, and the counterexample line shows the values.
        /// </summary>
        private static IEnumerable<string> CounterexampleAsCSharpLines(Counterexample<object?> counterexample)
        {
            var values = counterexample.PresentedInput;
            if (values.Count == 0) return Array.Empty<string>();

            var lines = new List<string> { "Counterexample as C#:" };
            foreach (var value in values)
            {
                if (!CSharpExpression.TryFormat(value, out var expression))
                {
                    return Array.Empty<string>();
                }

                lines.Add($"    {expression}");
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

        private static string ExceptionLine(Exception ex) => $"---- {ex.Message}";

        /// <summary>
        /// Gives other names to the marker lines of the readable text: the text of a value, an example, a label, or an
        /// exception can hold a line <c>&lt;galaxycheck-failure&gt;</c>, for example the block of a property that the
        /// test asserts inside. So the message holds one real block, the last one.
        /// </summary>
        private static string RenameMarkerLines(string text)
        {
            var (lines, breaks) = SplitLines(text);
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

            var result = new StringBuilder(text.Length + 64);
            for (var i = 0; i < lines.Length; i++)
            {
                result.Append(lines[i]);
                if (i < breaks.Length)
                {
                    result.Append(breaks[i]);
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Splits the text at each line break that a line reader can see: <c>\r\n</c>, <c>\n</c>, <c>\r</c>,
        /// <c>\v</c>, <c>\f</c>, U+0085, U+2028, and U+2029. A <c>\r\n</c> or a <c>\n</c> becomes
        /// <see cref="Environment.NewLine"/>, and each other break stays.
        /// </summary>
        private static (string[] Lines, string[] Breaks) SplitLines(string text)
        {
            var lines = new List<string>();
            var breaks = new List<string>();
            var start = 0;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c is '\n' or '\r' or '\v' or '\f' or '\u0085' or '\u2028' or '\u2029')
                {
                    lines.Add(text.Substring(start, i - start));
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        breaks.Add(Environment.NewLine);
                        i++;
                    }
                    else
                    {
                        breaks.Add(c == '\n' ? Environment.NewLine : c.ToString());
                    }

                    start = i + 1;
                }
            }

            lines.Add(text.Substring(start));
            return (lines.ToArray(), breaks.ToArray());
        }
    }
}
