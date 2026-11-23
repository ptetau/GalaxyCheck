using GalaxyCheck.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace GalaxyCheck.Internal
{
    internal class GlobalFailureMemoryConfiguration : IGlobalFailureMemoryConfiguration
    {
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// The saved failure of one test: the replay string under the full name of the test, in the file
    /// <c>.galaxycheck/failures.json</c> of the test project.
    /// </summary>
    internal sealed class SavedFailure
    {
        private SavedFailure(string filePath, string testName, bool isShared)
        {
            FilePath = filePath;
            TestName = testName;
            IsShared = isShared;
        }

        public string FilePath { get; }

        /// <summary>The full name of the test. It is a key of the JSON object, never a path.</summary>
        public string TestName { get; }

        /// <summary>
        /// True when the key can belong to several rows or fixtures of the test: a value entered the key as its type
        /// name, and the framework gave no index of its row or fixture. A saved replay that passes then stays, as it
        /// can be the failure of another row.
        /// </summary>
        public bool IsShared { get; }

        /// <returns>The saved failure of the test, or null when the memory is off or the project has no folder.</returns>
        public static SavedFailure? For(
            Type testClassType,
            MethodInfo testMethodInfo,
            object?[]? testMethodArguments,
            IGlobalFailureMemoryConfiguration config,
            object?[]? testClassArguments = null,
            string? rowIndex = null,
            string? fixtureIndex = null)
        {
            if (!config.Enabled)
            {
                return null;
            }

            var projectFolder = FailureFile.ProjectFolder(testClassType.Assembly);
            if (projectFolder == null)
            {
                return null;
            }

            var isShared = false;
            var testName = FullTestName(
                testClassType, testMethodInfo, testMethodArguments, testClassArguments, rowIndex, fixtureIndex, () => isShared = true);
            return new SavedFailure(
                Path.Combine(projectFolder, FailureFile.FolderName, FailureFile.FileName),
                testName,
                isShared);
        }

        /// <summary>
        /// The key of a test: <c>Namespace.Class.Method</c>, with the arguments of a parameterized class after the
        /// class, and the values of a data row after the method, for example <c>Ns.Fixture(5).Method(1, "a")</c>.
        /// A value that enters as its type name also holds the index of its row or fixture, when the framework gives
        /// one, for example <c>Ns.Class.Method(Ns.Db#1)</c>.
        /// </summary>
        /// <param name="rowIndex">The stable index of the data row among the rows of the test, or null.</param>
        /// <param name="fixtureIndex">The stable index of the fixture among the fixtures of the class, or null.</param>
        /// <param name="onShared">Called when a value enters as its type name with no index.</param>
        public static string FullTestName(
            Type testClassType,
            MethodInfo testMethodInfo,
            object?[]? testMethodArguments,
            object?[]? testClassArguments = null,
            string? rowIndex = null,
            string? fixtureIndex = null,
            Action? onShared = null) =>
            $"{TypeKeyName(testClassType)}{Arguments(testClassArguments, fixtureIndex, onShared)}" +
            $".{testMethodInfo.Name}{Arguments(testMethodArguments, rowIndex, onShared)}";

        private static string Arguments(object?[]? arguments, string? index, Action? onShared)
        {
            if (arguments == null || arguments.Length == 0)
            {
                return "";
            }

            return $"({string.Join(", ", arguments.Select(argument => KeyText(argument, index, onShared)))})";
        }

        /// <summary>
        /// The text of one argument in the key. Only a value with a stable text enters the key: null, a bool, a
        /// number, a char, a string, an enum, or a type. Any other value enters as its type name, as its text can
        /// change between runs (for example a record that holds a random port), and the key must not change. The
        /// type name holds the index of the row or fixture, so that two rows with values of one type keep two keys.
        /// </summary>
        private static string KeyText(object? argument, string? index, Action? onShared)
        {
            if (argument is Type type)
            {
                return $"typeof({TypeKeyName(type)})";
            }

            if (HasStableText(argument) && CSharpExpression.TryFormat(argument, out var expression))
            {
                return expression;
            }

            var typeName = TypeKeyName(argument!.GetType());
            if (index == null)
            {
                onShared?.Invoke();
                return typeName;
            }

            return $"{typeName}#{index}";
        }

        private static bool HasStableText(object? argument) => argument switch
        {
            null or bool or char or string or Enum => true,
            sbyte or byte or short or ushort or int or uint or long or ulong => true,
            float or double or decimal or nint or nuint => true,
            _ => false
        };

        /// <summary>The name of a type with its namespace and type arguments, and no assembly version.</summary>
        internal static string TypeKeyName(Type type)
        {
            if (type.IsArray)
            {
                return $"{TypeKeyName(type.GetElementType()!)}[{new string(',', type.GetArrayRank() - 1)}]";
            }

            if (type.IsGenericParameter)
            {
                return type.Name;
            }

            var name = type.IsNested && type.DeclaringType != null
                ? $"{TypeKeyName(type.DeclaringType)}+{type.Name}"
                : type.Namespace == null ? type.Name : $"{type.Namespace}.{type.Name}";

            if (!type.IsGenericType)
            {
                return name;
            }

            var tick = name.LastIndexOf('`');
            var baseName = tick >= 0 ? name.Substring(0, tick) : name;
            var typeArguments = type.IsGenericTypeDefinition
                ? string.Join(",", type.GetGenericArguments().Select(_ => ""))
                : string.Join(", ", type.GetGenericArguments().Select(TypeKeyName));
            return $"{baseName}<{typeArguments}>";
        }

        /// <returns>The saved replay string, or null.</returns>
        public string? Load() => FailureFile.Read(FilePath).TryGetValue(TestName, out var replay) ? replay : null;

        public void Save(string replay) => FailureFile.Update(FilePath, entries =>
        {
            if (entries.TryGetValue(TestName, out var existing) && existing == replay)
            {
                return false;
            }

            entries[TestName] = replay;
            return true;
        });

        public void Remove() => FailureFile.Update(FilePath, entries => entries.Remove(TestName));
    }

    /// <summary>
    /// Reads and writes the failure file as plain JSON data. Tests of one project can run in parallel, and several
    /// processes (one for each target framework) can share the file, so each change holds an exclusive lock on the
    /// file while it reads, changes, and writes the whole object.
    /// </summary>
    internal static class FailureFile
    {
        public const string FolderName = ".galaxycheck";
        public const string FileName = "failures.json";

        private const long MaxFileBytes = 1024 * 1024;
        private const int MaxAttempts = 200;

        private static readonly string[] ProjectFileExtensions = { ".csproj", ".fsproj", ".vbproj" };
        private static readonly ConcurrentDictionary<Assembly, string?> ProjectFolders = new();
        private static readonly object Gate = new();

        /// <summary>The key of the assembly metadata that the build of a GalaxyCheck package records.</summary>
        public const string ProjectFolderMetadataKey = "GalaxyCheck.ProjectFolder";

        private static readonly ConcurrentDictionary<Assembly, bool> ReportedMissingFolders = new();

        /// <summary>
        /// The folder of the test project: the folder that the build recorded in the assembly metadata, or else the
        /// nearest folder above the test assembly that holds the project file of the assembly. Never the folder of
        /// another project.
        /// </summary>
        public static string? ProjectFolder(Assembly assembly) => ProjectFolders.GetOrAdd(assembly, FindProjectFolder);

        /// <returns>
        /// The note that the failure memory found no project folder for the assembly, the first time only; else null.
        /// </returns>
        public static string? TakeMissingFolderNote(Assembly assembly)
        {
            if (ProjectFolder(assembly) != null || !ReportedMissingFolders.TryAdd(assembly, true))
            {
                return null;
            }

            return $"GalaxyCheck did not save this failure: it found no project folder for the assembly " +
                $"'{assembly.GetName().Name}', so the failure memory ({FolderName}/{FileName}) is off." +
                Environment.NewLine +
                "The build records the project folder in the assembly metadata, but it records nothing when the " +
                "project sets GenerateAssemblyInfo=false, or when it references GalaxyCheck as a project (not a " +
                "package) and builds to a custom output folder." + Environment.NewLine +
                "Fix: remove GenerateAssemblyInfo=false, or reference a GalaxyCheck package, or build the test " +
                "assembly to a folder below its project file.";
        }

        private static string? FindProjectFolder(Assembly assembly)
        {
            var recorded = RecordedProjectFolder(assembly);
            if (recorded != null)
            {
                return recorded;
            }

            string? location;
            try
            {
                location = assembly.Location;
            }
            catch (NotSupportedException)
            {
                return null;
            }

            if (string.IsNullOrEmpty(location))
            {
                return null;
            }

            // The nearest folder with the project file of the assembly. Else, for a project whose assembly name
            // differs from its file name, the first folder above the assembly that holds any project file, when it
            // holds exactly one. Never a folder above that first one.
            var assemblyName = assembly.GetName().Name;
            string? fallback = null;
            var passedFirstProjectFolder = false;
            for (var folder = new FileInfo(location).Directory; folder != null; folder = folder.Parent)
            {
                try
                {
                    var projectFiles = folder.EnumerateFiles()
                        .Where(file => ProjectFileExtensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase))
                        .ToList();
                    if (projectFiles.Any(file =>
                            string.Equals(Path.GetFileNameWithoutExtension(file.Name), assemblyName, StringComparison.OrdinalIgnoreCase)))
                    {
                        return folder.FullName;
                    }

                    if (projectFiles.Count > 0 && !passedFirstProjectFolder)
                    {
                        passedFirstProjectFolder = true;
                        fallback = projectFiles.Count == 1 ? folder.FullName : null;
                    }
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is System.Security.SecurityException)
                {
                }
            }

            return fallback;
        }

        /// <summary>The folder that the build recorded, when it exists on this machine.</summary>
        private static string? RecordedProjectFolder(Assembly assembly)
        {
            try
            {
                var recorded = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                    .LastOrDefault(attribute => attribute.Key == ProjectFolderMetadataKey)?.Value;
                return !string.IsNullOrWhiteSpace(recorded) && Directory.Exists(recorded) ? Path.GetFullPath(recorded) : null;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is System.Security.SecurityException || ex is ArgumentException || ex is NotSupportedException)
            {
                return null;
            }
        }

        public static IReadOnlyDictionary<string, string> Read(string filePath)
        {
            lock (Gate)
            {
                return WithRetry(() =>
                {
                    try
                    {
                        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        return Parse(stream) ?? new SortedDictionary<string, string>(StringComparer.Ordinal);
                    }
                    catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
                    {
                        return new SortedDictionary<string, string>(StringComparer.Ordinal);
                    }
                });
            }
        }

        /// <param name="change">Changes the entries, and returns true when it changed one.</param>
        public static void Update(string filePath, Func<SortedDictionary<string, string>, bool> change)
        {
            lock (Gate)
            {
                WithRetry(() =>
                {
                    if (!File.Exists(filePath))
                    {
                        var entries = new SortedDictionary<string, string>(StringComparer.Ordinal);
                        if (!change(entries))
                        {
                            return 0;
                        }

                        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                    }

                    using var stream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    var current = stream.Length == 0
                        ? new SortedDictionary<string, string>(StringComparer.Ordinal)
                        : Parse(stream);
                    if (current == null || !change(current))
                    {
                        // A file that is not a JSON object of strings is left as it is, so no entry is lost.
                        return 0;
                    }

                    var bytes = JsonSerializer.SerializeToUtf8Bytes(current, new JsonSerializerOptions { WriteIndented = true });
                    stream.SetLength(0);
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Write(Encoding.UTF8.GetBytes(Environment.NewLine));
                    return 0;
                });
            }
        }

        /// <returns>The entries, or null when the file is too large or is not a JSON object of strings.</returns>
        private static SortedDictionary<string, string>? Parse(FileStream stream)
        {
            if (stream.Length > MaxFileBytes)
            {
                return null;
            }

            try
            {
                var entries = JsonSerializer.Deserialize<Dictionary<string, string>>(stream);
                return entries == null ? null : new SortedDictionary<string, string>(entries, StringComparer.Ordinal);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>Retries an access that another process blocks, for a few seconds.</summary>
        private static T WithRetry<T>(Func<T> access)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return access();
                }
                catch (IOException ex) when (ex is not FileNotFoundException && ex is not DirectoryNotFoundException && attempt < MaxAttempts)
                {
                    Thread.Sleep(Random.Shared.Next(5, 30));
                }
            }
        }
    }
}
