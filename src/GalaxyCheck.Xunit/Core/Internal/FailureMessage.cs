using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GalaxyCheck.Internal
{
    /// <summary>How a framework runs one test from the command line, in the folder of the test project.</summary>
    internal enum RerunStyle
    {
        /// <summary><c>dotnet test --filter</c>, for xunit, NUnit, and MSTest.</summary>
        DotnetTestFilter,

        /// <summary><c>dotnet run -- --treenode-filter</c>, for TUnit.</summary>
        TUnitTreeNodeFilter,
    }

    /// <summary>The test of a failure: its full name, and one command line that runs only that test.</summary>
    internal sealed record FailedTestIdentity(string Test, string Rerun);

    /// <summary>
    /// Changes the failure message of a property in a framework: adds the test and its rerun command to the JSON block
    /// of the message, and limits the lines of the message in the quiet mode (<c>GALAXYCHECK_AGENT=1</c>).
    /// </summary>
    internal static class FailureMessage
    {
        private const string StartLine = "<galaxycheck-failure>";
        private const string EndLine = "</galaxycheck-failure>";

        /// <summary>
        /// The most lines of the message that GalaxyCheck gives a framework in the quiet mode. A framework can add a
        /// line or two, for example the name of the exception type.
        /// </summary>
        private const int MaxAgentLines = 36;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        public static bool AgentModeIsOn
        {
            get
            {
                string? value;
                try
                {
                    value = Environment.GetEnvironmentVariable("GALAXYCHECK_AGENT");
                }
                catch (System.Security.SecurityException)
                {
                    return false;
                }

                return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
            }
        }

        public static FailedTestIdentity Identify(
            RerunStyle style,
            Type testClassType,
            MethodInfo testMethodInfo,
            object?[]? testMethodArguments,
            object?[]? testClassArguments,
            string? rowIndex,
            string? fixtureIndex)
        {
            var test = SavedFailure.FullTestName(
                testClassType, testMethodInfo, testMethodArguments, testClassArguments, rowIndex, fixtureIndex);
            var hasRow = testMethodArguments is { Length: > 0 };
            var hasFixture = testClassArguments is { Length: > 0 };

            var rerun = style == RerunStyle.TUnitTreeNodeFilter
                ? TUnitRerun(testClassType, testMethodInfo)
                : DotnetTestRerun(testClassType, testMethodInfo, hasRow, hasFixture);

            return new FailedTestIdentity(test, rerun);
        }

        /// <summary>
        /// The command for a VSTest adapter. The fully qualified name of a test is <c>Namespace.Class.Method</c>. NUnit
        /// adds the values of a row or a fixture, so such a test matches by a part of the name.
        /// </summary>
        private static string DotnetTestRerun(Type testClassType, MethodInfo testMethodInfo, bool hasRow, bool hasFixture)
        {
            var className = testClassType.FullName ?? testClassType.Name;
            var filter = hasFixture
                ? $"FullyQualifiedName~{className}&FullyQualifiedName~.{testMethodInfo.Name}"
                : hasRow
                    ? $"FullyQualifiedName~{className}.{testMethodInfo.Name}"
                    : $"FullyQualifiedName={className}.{testMethodInfo.Name}";

            return $"dotnet test --filter {Quote(filter)}";
        }

        /// <summary>
        /// The command for TUnit, whose tree node path is <c>/Assembly/Namespace/Class/Method</c>. The framework of the
        /// running test assembly picks the target of a project with several targets.
        /// </summary>
        private static string TUnitRerun(Type testClassType, MethodInfo testMethodInfo)
        {
            var assembly = testClassType.Assembly.GetName().Name ?? "*";
            var ns = string.IsNullOrEmpty(testClassType.Namespace) ? "*" : testClassType.Namespace;
            var filter = $"/{assembly}/{ns}/{ClassName(testClassType)}/{testMethodInfo.Name}";
            var framework = TargetFramework(testClassType.Assembly);
            var frameworkOption = framework == null ? "" : $" -f {framework}";

            return $"dotnet run{frameworkOption} -- --treenode-filter {Quote(filter)}";
        }

        private static string ClassName(Type type)
        {
            var name = type.Name;
            var tick = name.IndexOf('`');
            return tick >= 0 ? name.Substring(0, tick) : name;
        }

        /// <summary>The target framework moniker of an assembly, for example <c>net11.0</c>, or null.</summary>
        private static string? TargetFramework(Assembly assembly)
        {
            var frameworkName = assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
            if (frameworkName == null)
            {
                return null;
            }

            const string Prefix = ".NETCoreApp,Version=v";
            if (!frameworkName.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return null;
            }

            var version = frameworkName.Substring(Prefix.Length);
            return version.All(c => char.IsDigit(c) || c == '.') && version.Length > 0 ? $"net{version}" : null;
        }

        /// <summary>Quotes a filter that holds a char that a shell reads, so that the command runs as it is.</summary>
        private static string Quote(string value) =>
            value.All(c => char.IsLetterOrDigit(c) || "_.=~/+-:".Contains(c))
                ? value
                : "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

        /// <returns>
        /// The message with <c>test</c> and <c>rerun</c> in its JSON block, cut to the line limit of the quiet mode.
        /// A message with no block only gets the line limit.
        /// </returns>
        public static string Complete(string message, FailedTestIdentity? identity)
        {
            var lines = message.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();
            var start = lines.IndexOf(StartLine);
            var hasBlock = start >= 0 && start + 2 < lines.Count && lines[start + 2] == EndLine;

            if (hasBlock && identity != null)
            {
                lines[start + 1] = WithIdentity(lines[start + 1], identity);
            }

            if (AgentModeIsOn && lines.Count > MaxAgentLines)
            {
                lines = hasBlock ? CutAround(lines, start) : Cut(lines, MaxAgentLines);
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static string WithIdentity(string json, FailedTestIdentity identity)
        {
            try
            {
                if (JsonNode.Parse(json) is JsonObject record)
                {
                    record["test"] = identity.Test;
                    record["rerun"] = identity.Rerun;
                    return record.ToJsonString(JsonOptions);
                }
            }
            catch (JsonException)
            {
            }

            return json;
        }

        /// <summary>Keeps the JSON block, and as many lines before and after it as the limit allows.</summary>
        private static List<string> CutAround(List<string> lines, int start)
        {
            var block = lines.GetRange(start, 3);
            var before = lines.Take(start).ToList();
            var after = lines.Skip(start + 3).ToList();

            // The block, a blank line before it, and a line that says how many lines are hidden.
            var room = MaxAgentLines - block.Count - 2;
            var keptBefore = before.Take(Math.Min(before.Count, room)).ToList();
            var keptAfter = after.Take(Math.Max(0, room - keptBefore.Count)).ToList();
            var hidden = before.Count - keptBefore.Count + after.Count - keptAfter.Count;

            var result = new List<string>(keptBefore);
            if (hidden > 0)
            {
                result.Add($"... ({hidden} more lines are hidden in the quiet mode)");
            }

            if (result.Count == 0 || result[result.Count - 1].Length != 0)
            {
                result.Add("");
            }

            result.AddRange(block);
            result.AddRange(keptAfter);
            return result;
        }

        private static List<string> Cut(List<string> lines, int max)
        {
            var result = lines.Take(max - 1).ToList();
            result.Add($"... ({lines.Count - (max - 1)} more lines are hidden in the quiet mode)");
            return result;
        }
    }
}
