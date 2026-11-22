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
        private SavedFailure(string filePath, string testName)
        {
            FilePath = filePath;
            TestName = testName;
        }

        public string FilePath { get; }

        /// <summary>The full name of the test. It is a key of the JSON object, never a path.</summary>
        public string TestName { get; }

        /// <returns>The saved failure of the test, or null when the memory is off or the project has no folder.</returns>
        public static SavedFailure? For(
            Type testClassType,
            MethodInfo testMethodInfo,
            object?[]? testMethodArguments,
            IGlobalFailureMemoryConfiguration config)
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

            return new SavedFailure(
                Path.Combine(projectFolder, FailureFile.FolderName, FailureFile.FileName),
                FullTestName(testClassType, testMethodInfo, testMethodArguments));
        }

        public static string FullTestName(Type testClassType, MethodInfo testMethodInfo, object?[]? testMethodArguments)
        {
            var name = $"{testClassType.FullName ?? testClassType.Name}.{testMethodInfo.Name}";
            if (testMethodArguments == null || testMethodArguments.Length == 0)
            {
                return name;
            }

            var arguments = testMethodArguments.Select(argument =>
                CSharpExpression.TryFormat(argument, out var expression) ? expression : argument?.ToString() ?? "null");
            return $"{name}({string.Join(", ", arguments)})";
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

        /// <summary>
        /// The folder of the test project: the nearest folder above the test assembly that holds the project file of
        /// the assembly, or else the nearest folder that holds a project file.
        /// </summary>
        public static string? ProjectFolder(Assembly assembly) => ProjectFolders.GetOrAdd(assembly, FindProjectFolder);

        private static string? FindProjectFolder(Assembly assembly)
        {
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

            var assemblyName = assembly.GetName().Name;
            string? firstFolderWithProject = null;
            for (var folder = new FileInfo(location).Directory; folder != null; folder = folder.Parent)
            {
                string[] projectFiles;
                try
                {
                    projectFiles = folder.EnumerateFiles()
                        .Where(file => ProjectFileExtensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase))
                        .Select(file => Path.GetFileNameWithoutExtension(file.Name))
                        .ToArray();
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is System.Security.SecurityException)
                {
                    continue;
                }

                if (projectFiles.Contains(assemblyName, StringComparer.OrdinalIgnoreCase))
                {
                    return folder.FullName;
                }

                if (projectFiles.Length > 0)
                {
                    firstFolderWithProject ??= folder.FullName;
                }
            }

            return firstFolderWithProject;
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
