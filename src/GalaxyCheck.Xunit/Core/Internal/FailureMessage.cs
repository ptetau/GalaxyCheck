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

        /// <param name="frameworkTestName">
        /// The full name of the test as the framework reports it to the test host, or null to build it from the
        /// class and the method. NUnit gives it, as its name holds the values of a row or a fixture.
        /// </param>
        /// <param name="dotnetTestFilter">
        /// The <c>dotnet test</c> filter that selects the test, before the shell quoting, or null to build it from the
        /// class and the method. NUnit gives it, from <see cref="FilterEquals"/> and <see cref="FilterContainsAll"/>.
        /// </param>
        public static FailedTestIdentity Identify(
            RerunStyle style,
            Type testClassType,
            MethodInfo testMethodInfo,
            object?[]? testMethodArguments,
            object?[]? testClassArguments,
            string? rowIndex,
            string? fixtureIndex,
            string? frameworkTestName = null,
            string? dotnetTestFilter = null)
        {
            var test = frameworkTestName ?? SavedFailure.FullTestName(
                testClassType, testMethodInfo, testMethodArguments, testClassArguments, rowIndex, fixtureIndex);

            var rerun = style == RerunStyle.TUnitTreeNodeFilter
                ? TUnitRerun(testClassType, testMethodInfo)
                : DotnetTestRerun(dotnetTestFilter ?? MethodFilter(testClassType, testMethodInfo));

            return new FailedTestIdentity(test, rerun);
        }

        /// <summary>
        /// The filter of a test of xunit or MSTest: an exact match on <c>Namespace.Class.Method</c>. Each row of a
        /// method has that name, so the filter selects the rows of that method, and no other method. A name that the
        /// command line cannot carry selects each test of the class.
        /// </summary>
        private static string MethodFilter(Type testClassType, MethodInfo testMethodInfo)
        {
            var className = testClassType.FullName ?? testClassType.Name;
            var name = $"{className}.{testMethodInfo.Name}";
            return CanFilterExactly(name) ? FilterEquals("FullyQualifiedName", name) : ClassFilter(className);
        }

        /// <summary>
        /// The filter of an xunit test: the method filter, and for a data row an exact match on the display name of
        /// the row, so that it selects only that row. A display name that the command line cannot carry gets the rows
        /// of the method.
        /// </summary>
        public static string XunitFilter(Type testClassType, MethodInfo testMethodInfo, bool isDataRow, string? displayName)
        {
            var methodFilter = MethodFilter(testClassType, testMethodInfo);
            var name = $"{testClassType.FullName ?? testClassType.Name}.{testMethodInfo.Name}";
            return isDataRow && displayName != null && CanFilterExactly(name) && CanFilterExactly(displayName)
                ? methodFilter + "&" + FilterEquals("DisplayName", displayName)
                : methodFilter;
        }

        /// <summary>
        /// The filter of each test of a class, for a test name that the command line cannot carry: the full name
        /// contains <c>Namespace.Class.</c>.
        /// </summary>
        public static string ClassFilter(string classFullName) =>
            CanFilterExactly(classFullName)
                ? $"FullyQualifiedName~{EscapeFilterValue(classFullName + ".", VsTestSpecial)}"
                : FilterContainsAll("FullyQualifiedName", classFullName);

        /// <summary>
        /// True when <c>dotnet test --filter</c> can carry the value in an exact match. The test host reads a
        /// backslash before a quote as an escape of the quote, so such a value matches no test. Each other value,
        /// also with a space or a single quote, matches exactly.
        /// </summary>
        public static bool CanFilterExactly(string value)
        {
            for (var i = 1; i < value.Length; i++)
            {
                if (value[i] == '"' && value[i - 1] == '\\')
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>An exact match of a filter property, with each special char of the filter syntax escaped.</summary>
        public static string FilterEquals(string property, string value) =>
            $"{property}={EscapeFilterValue(value, VsTestSpecial)}";

        /// <summary>
        /// The filter for a class name that <see cref="CanFilterExactly"/> rejects: the name must contain each part of
        /// the name between a quote or a space. It selects the tests of the class, and it can also select a test whose
        /// name holds all of the parts.
        /// </summary>
        public static string FilterContainsAll(string property, string value)
        {
            var parts = value
                .Split(new[] { '"' }, StringSplitOptions.None)
                .SelectMany(part => part.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                .Where(part => part.Length > 0)
                .ToList();

            return parts.Count == 0
                ? $"{property}~{EscapeFilterValue(value.Trim(), VsTestSpecial)}"
                : string.Join("&", parts.Select(part => $"{property}~{EscapeFilterValue(part, VsTestSpecial)}"));
        }

        private const string VsTestSpecial = "\\()&|=!~";

        /// <summary>
        /// The command for a VSTest adapter. <c>dotnet test</c> passes the filter to MSBuild, which reads <c>%</c>,
        /// <c>;</c>, <c>"</c>, <c>$</c>, and <c>@</c>, so each of them is written as its <c>%XX</c> escape.
        /// </summary>
        private static string DotnetTestRerun(string filter)
        {
            var forMsBuild = new System.Text.StringBuilder(filter.Length);
            foreach (var c in filter)
            {
                forMsBuild.Append(c switch
                {
                    '%' => "%25",
                    ';' => "%3B",
                    '"' => "%22",
                    '$' => "%24",
                    '@' => "%40",
                    _ => c.ToString(),
                });
            }

            return $"dotnet test --filter {Quote(forMsBuild.ToString())}";
        }

        /// <summary>
        /// The command for TUnit, whose tree node path is <c>/Assembly/Namespace/Class/Method</c>, with
        /// <c>Outer+Inner</c> for a nested class. TUnit cannot select one data row, so the command runs the rows of
        /// that method. The framework of the running test assembly picks the target of a project with several targets.
        /// </summary>
        private static string TUnitRerun(Type testClassType, MethodInfo testMethodInfo)
        {
            const string Special = "\\/*[]()&|=!";
            var assembly = EscapeFilterValue(testClassType.Assembly.GetName().Name ?? "*", Special);
            var ns = string.IsNullOrEmpty(testClassType.Namespace) ? "*" : EscapeFilterValue(testClassType.Namespace!, Special);
            var filter = $"/{assembly}/{ns}/{EscapeFilterValue(ClassName(testClassType), Special)}/{EscapeFilterValue(testMethodInfo.Name, Special)}";
            var framework = TargetFramework(testClassType.Assembly);
            var frameworkOption = framework == null ? "" : $" -f {framework}";

            return $"dotnet run{frameworkOption} -- --treenode-filter {Quote(filter)}";
        }

        /// <summary>The name of a class in the tree of TUnit: the names of its outer classes and its name, joined by <c>+</c>.</summary>
        private static string ClassName(Type type)
        {
            var name = type.Name;
            var tick = name.IndexOf('`');
            name = tick >= 0 ? name.Substring(0, tick) : name;
            return type.IsNested && type.DeclaringType != null && !type.IsGenericParameter
                ? $"{ClassName(type.DeclaringType)}+{name}"
                : name;
        }

        /// <summary>Puts a backslash before each char of a filter value that the filter syntax reads.</summary>
        private static string EscapeFilterValue(string value, string special)
        {
            var result = new System.Text.StringBuilder(value.Length);
            foreach (var c in value)
            {
                if (special.IndexOf(c) >= 0)
                {
                    result.Append('\\');
                }

                result.Append(c);
            }

            return result.ToString();
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

        /// <summary>
        /// Quotes one argument of a command for a POSIX shell (bash, zsh): a value of plain chars stays as it is, and
        /// any other value goes in single quotes, with each <c>'</c> written as <c>'\''</c>. The shell expands nothing
        /// in single quotes, so no test name can change the command.
        /// </summary>
        internal static string Quote(string value) =>
            value.Length > 0 && value.All(c => (c < 128 && char.IsLetterOrDigit(c)) || "_.=/+-:,".IndexOf(c) >= 0)
                ? value
                : "'" + value.Replace("'", "'\\''") + "'";

        /// <returns>
        /// The message with <c>test</c> and <c>rerun</c> in its JSON block, cut to the line limit of the quiet mode.
        /// A message with no block only gets the line limit. The block is the last one of the message: the exception
        /// text of the counterexample comes before it, and indents each marker line that it holds.
        /// </returns>
        /// <param name="example">
        /// The attribute of a failed example, for example <c>[Example(20)]</c>, or null for a generated failure. An
        /// example has no replay string, seed, or size, so the block holds null for each, and the attribute in
        /// <c>example</c>.
        /// </param>
        public static string Complete(string message, FailedTestIdentity? identity, string? example = null)
        {
            var lines = message.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();
            var start = lines.LastIndexOf(StartLine);
            var hasBlock = start >= 0 && start + 2 < lines.Count && lines[start + 2] == EndLine;

            if (hasBlock && (identity != null || example != null))
            {
                lines[start + 1] = WithIdentity(lines[start + 1], identity, example);
            }

            if (AgentModeIsOn && lines.Count > MaxAgentLines)
            {
                lines = hasBlock ? CutAround(lines, start) : Cut(lines, MaxAgentLines);
            }

            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>The message of a failure with no JSON block, for example of a [Sample] test, cut to the line limit of the quiet mode.</summary>
        public static string Limit(string message) => Complete(message, identity: null);

        private static string WithIdentity(string json, FailedTestIdentity? identity, string? example)
        {
            try
            {
                if (JsonNode.Parse(json) is JsonObject record)
                {
                    if (example != null)
                    {
                        record["replay"] = null;
                        record["seed"] = null;
                        record["size"] = null;
                        record["example"] = example;
                    }

                    if (identity != null)
                    {
                        record["test"] = identity.Test;
                        record["rerun"] = identity.Rerun;
                    }

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
