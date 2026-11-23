using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;

namespace Tests.IntegrationTests
{
    public record Invocation(object[] InjectedParameters);

    public record TestResult(string TestName, string Outcome, string Message, string Output);

    public abstract class TestSuiteFixture : IAsyncLifetime
    {
        private static readonly SemaphoreSlim Lock = new(1, 1);

        private readonly string _currentDirectory = Directory.GetCurrentDirectory();
        private readonly string _testReportFileName;

        public TestSuiteFixture(string testSuiteName)
        {
            TestSuiteName = testSuiteName;
            _testReportFileName = Path.Combine(_currentDirectory, $"test_result_{testSuiteName}_{DateTime.UtcNow.Ticks}.trx");
        }

        public string TestSuiteName { get; }

        public ImmutableList<TestResult> TestResults { get; private set; } = default!;

        public IReadOnlyDictionary<string, string> TestNameToTestResult { get; private set; } = default!;

        public string TestReport { get; private set; } = default!;

        public async Task InitializeAsync()
        {
            await Lock.WaitAsync();

            try
            {
                var currentProjectDir = Enumerable
                    .Range(0, 3)
                    .Aggregate(new DirectoryInfo(_currentDirectory), (acc, _) => acc.Parent!);
                var integrationProjectDir = Path.Combine(currentProjectDir.FullName, "samples", TestSuiteName);

                // The test hosts of net10.0 and net11.0 run at the same time, and each builds and runs the sample
                // projects. The samples share the library projects, and two builds of one project at once break each
                // other, so one lock file for all samples lets one host in.
                using var sampleLock = await AcquireSampleLock(integrationProjectDir);

                // The failure memory of an earlier run changes the messages of this run, so it starts with none.
                var failureMemoryDir = Path.Combine(integrationProjectDir, ".galaxycheck");
                if (Directory.Exists(failureMemoryDir))
                {
                    Directory.Delete(failureMemoryDir, recursive: true);
                }

                var testProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"test {integrationProjectDir} --logger \"trx;LogFileName={_testReportFileName}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                });

                var standardOutput = await testProcess!.StandardOutput.ReadToEndAsync();
                var standardError = await testProcess!.StandardError.ReadToEndAsync();

                if (!File.Exists(_testReportFileName))
                {
                    throw new InvalidOperationException(
                        $"The run of {integrationProjectDir} wrote no test report.\n{standardOutput}\n{standardError}");
                }

                TestReport = await File.ReadAllTextAsync(_testReportFileName);

                var testResults =
                    from descendent in XElement.Parse(TestReport).Descendants()
                    where descendent.Name.LocalName == "UnitTestResult"
                    select ElementToTestResult(descendent);

                TestResults = testResults.ToImmutableList();

                TestNameToTestResult = TestResults
                    .OrderBy(x => x.TestName)
                    .GroupBy(x => x.TestName.Contains('(') ? x.TestName[..x.TestName.IndexOf('(')] : x.TestName)
                    .SelectMany(g => g.Select((x, i) => (TestName: $"{g.Key}_{i}", TestResult: x)))
                    .ToDictionary(
                        x => x.TestName,
                        x => string.Join("\n", $"Outcome: {x.TestResult.Outcome}", "", "Output:", "", x.TestResult.Output));
            }
            finally
            {
                Lock.Release();
            }
        }

        public Task DisposeAsync() => Task.CompletedTask;

        private static async Task<FileStream> AcquireSampleLock(string integrationProjectDir)
        {
            var path = Path.Combine(Path.GetTempPath(), "galaxycheck-integration-samples.lock");
            var deadline = DateTime.UtcNow.AddMinutes(10);
            while (true)
            {
                try
                {
                    return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                }
                catch (IOException) when (DateTime.UtcNow < deadline)
                {
                    await Task.Delay(250);
                }
            }
        }

        private static TestResult ElementToTestResult(XElement element)
        {
            var attributes = element.Attributes();

            var message = (
                from desc in element.Descendants()
                where desc.Name.LocalName.Contains("Message")
                select desc.Value).SingleOrDefault();

            var output = (
                from desc in element.Descendants()
                where desc.Name.LocalName.Contains("Output")
                select desc.Value).SingleOrDefault();
            // Windows writes "\r\n", and the snapshots hold "\n".
            output = output?.Replace("\r\n", "\n");
            if (output?.Contains("   at ") is true)
            {
                output = output[..output.IndexOf("   at ")]; // Scrub the stacktrace with local paths 😅
            }

            return new TestResult(
                attributes.Single(a => a.Name.LocalName.Contains("testName")).Value.Split('.').Last(),
                attributes.Single(a => a.Name.LocalName.Contains("outcome")).Value,
                message!,
                output!);
        }
    }
}
