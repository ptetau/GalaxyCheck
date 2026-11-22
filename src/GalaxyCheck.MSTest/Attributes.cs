using GalaxyCheck.Internal.Infrastructure.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

// The MSTest part of each attribute. The options are in the shared core. Each attribute is a TestMethodAttribute, so
// MSTest finds the method as a test with no [TestMethod] beside it, and it runs the property in place of one call of
// the method. A [DataRow] or [DynamicData] row gives the first parameters.

namespace GalaxyCheck
{
    public partial class PropertyAttribute : TestMethodAttribute
    {
        public PropertyAttribute([CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = -1)
            : base(callerFilePath, callerLineNumber)
        {
        }

        // MSTest skips an [Ignore] test before it runs, so the shared core never sees a skip reason.
        internal string? SkipReason => null;

        public override Task<TestResult[]> ExecuteAsync(ITestMethod testMethod) => MSTestAdapter.RunProperty(testMethod);
    }

    public partial class SampleAttribute
    {
        public SampleAttribute([CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = -1)
            : base(callerFilePath, callerLineNumber)
        {
        }
    }

    public sealed partial class GenSnapshotAttribute : TestMethodAttribute
    {
        public GenSnapshotAttribute([CallerFilePath] string? testFilePath = null, [CallerLineNumber] int callerLineNumber = -1)
            : base(testFilePath ?? "", callerLineNumber)
        {
            TestFileName = testFilePath;
        }

        public string? TestFileName { get; }

        internal string? SkipReason => null;

        public override Task<TestResult[]> ExecuteAsync(ITestMethod testMethod) => MSTestAdapter.RunGenSnapshot(testMethod);
    }
}
