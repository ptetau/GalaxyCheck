using GalaxyCheck.Internal.Infrastructure.NUnit;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using System.Runtime.CompilerServices;

// The NUnit part of each attribute. The options are in the shared core. Each attribute builds the test of its method
// (ISimpleTestBuilder), so NUnit needs no [Test] beside it, and wraps the command of the test (IWrapTestMethod), so
// that the property runs in place of one call of the method. A [TestCase] or [TestCaseSource] row builds its own test,
// and the attribute wraps its command in the same way.

namespace GalaxyCheck
{
    public partial class PropertyAttribute : NUnitAttribute, ISimpleTestBuilder, IImplyFixture, IWrapTestMethod, ITestAction
    {
        // NUnit skips an [Ignore] test before it runs, so the shared core never sees a skip reason.
        internal string? SkipReason => null;

        TestMethod ISimpleTestBuilder.BuildFrom(IMethodInfo method, Test? suite) => NUnitAdapter.BuildTestMethod(method, suite);

        TestCommand ICommandWrapper.Wrap(TestCommand command) => new PropertyCommand(command.Test);

        ActionTargets ITestAction.Targets => ActionTargets.Suite;

        void ITestAction.BeforeTest(ITest test) => NUnitAdapter.AcceptPartialRows(test);

        void ITestAction.AfterTest(ITest test)
        {
        }
    }

    public sealed partial class GenSnapshotAttribute : NUnitAttribute, ISimpleTestBuilder, IImplyFixture, IWrapTestMethod
    {
        /// <summary>Creates the attribute. The compiler passes the path of the test file.</summary>
        public GenSnapshotAttribute([CallerFilePath] string? testFilePath = null)
        {
            TestFileName = testFilePath;
        }

        /// <summary>The path of the file of the test method.</summary>
        public string? TestFileName { get; }

        internal string? SkipReason => null;

        TestMethod ISimpleTestBuilder.BuildFrom(IMethodInfo method, Test? suite) => NUnitAdapter.BuildTestMethod(method, suite);

        TestCommand ICommandWrapper.Wrap(TestCommand command) => new GenSnapshotCommand(command.Test);
    }
}
