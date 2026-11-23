using GalaxyCheck.Runners.Check;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace GalaxyCheck.Runners
{
    /// <summary>
    /// The machine-readable part of a failure message: one JSON object on one line, between the lines
    /// <c>&lt;galaxycheck-failure&gt;</c> and <c>&lt;/galaxycheck-failure&gt;</c>. JSON escapes each line break of a
    /// value, so a value cannot end the block.
    /// </summary>
    internal static class FailureRecord
    {
        public const string StartLine = "<galaxycheck-failure>";
        public const string EndLine = "</galaxycheck-failure>";

        private static readonly JsonWriterOptions WriterOptions = new()
        {
            // The block is plain text for a reader or an agent, never HTML, so C# expressions keep their < and >.
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        /// <summary>The kind of a generated failure: a test case failed.</summary>
        public const string GeneratedKind = "generated";

        /// <summary>The kind of a coverage failure: the run did not reach the share of a label of a cover rule.</summary>
        public const string CoverageKind = "coverage";

        /// <summary>The three lines of the block for a counterexample.</summary>
        public static IReadOnlyList<string> Lines(Counterexample<object?> counterexample, int iterations, int shrinks) =>
            new[] { StartLine, Json(counterexample, iterations, shrinks), EndLine };

        /// <summary>
        /// The three lines of the block for a coverage failure. It has no counterexample, so <c>counterexample</c>,
        /// <c>csharp</c>, <c>replay</c>, and <c>size</c> are null, and <c>cover</c> holds the label and the shares.
        /// </summary>
        public static IReadOnlyList<string> CoverageLines(CoverageFailure coverage, int iterations, int seed) =>
            new[] { StartLine, CoverageJson(coverage, iterations, seed), EndLine };

        private static string CoverageJson(CoverageFailure coverage, int iterations, int seed)
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, WriterOptions))
            {
                writer.WriteStartObject();
                writer.WriteNull("counterexample");
                writer.WriteNull("csharp");
                writer.WriteNull("replay");
                writer.WriteNumber("seed", seed);
                writer.WriteNull("size");
                writer.WriteNumber("iterations", iterations);
                writer.WriteNumber("shrinks", 0);
                writer.WriteString("kind", CoverageKind);
                writer.WriteStartObject("cover");
                writer.WriteString("label", coverage.Label);
                writer.WriteNumber("required", Math.Round(coverage.RequiredPercent, 1));
                writer.WriteNumber("actual", Math.Round(coverage.ActualPercent, 1));
                writer.WriteEndObject();
                writer.WriteEndObject();
            }

            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }

        private static string Json(Counterexample<object?> counterexample, int iterations, int shrinks)
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, WriterOptions))
            {
                var values = counterexample.PresentedInput;

                writer.WriteStartObject();

                writer.WriteStartArray("counterexample");
                foreach (var value in values)
                {
                    writer.WriteStringValue(Text(value));
                }
                writer.WriteEndArray();

                writer.WriteStartArray("csharp");
                foreach (var value in values)
                {
                    if (CSharpExpression.TryFormat(value, out var expression))
                    {
                        writer.WriteStringValue(expression);
                    }
                    else
                    {
                        writer.WriteNullValue();
                    }
                }
                writer.WriteEndArray();

                writer.WriteString("replay", counterexample.Replay);
                writer.WriteNumber("seed", counterexample.ReplayParameters.Seed);
                writer.WriteNumber("size", counterexample.ReplayParameters.Size.Value);
                writer.WriteNumber("iterations", iterations);
                writer.WriteNumber("shrinks", shrinks);
                writer.WriteString("kind", GeneratedKind);

                writer.WriteEndObject();
            }

            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }

        private static string Text(object? value)
        {
            try
            {
                return ExampleRenderer.RenderOne(value);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                // A value whose members throw when they are read still has a type name.
                return value?.GetType().Name ?? "null";
            }
        }
    }
}
