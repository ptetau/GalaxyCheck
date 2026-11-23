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

        /// <summary>The three lines of the block for a counterexample.</summary>
        public static IReadOnlyList<string> Lines(Counterexample<object?> counterexample, int iterations, int shrinks) =>
            new[] { StartLine, Json(counterexample, iterations, shrinks), EndLine };

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
