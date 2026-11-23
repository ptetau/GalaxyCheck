using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace GalaxyCheck.Internal.Infrastructure.NUnit
{
    internal static class NUnitAdapter
    {
        /// <summary>
        /// Builds the test of a property method with no data row. The property generates the parameters and checks
        /// the return value, so the method needs neither arguments nor a void return.
        /// </summary>
        public static TestMethod BuildTestMethod(IMethodInfo method, Test? suite)
        {
            var testMethod = new TestMethod(method, suite)
            {
                Name = method.Name,
            };
            testMethod.FullName = (suite?.FullName ?? method.TypeInfo.FullName) + "." + testMethod.Name;

            if (method.IsAbstract)
            {
                testMethod.MakeInvalid("Method is abstract");
            }
            else if (!method.IsPublic)
            {
                testMethod.MakeInvalid("Method is not public");
            }
            else
            {
                testMethod.ApplyAttributesToTest(method.MethodInfo);
            }

            return testMethod;
        }

        /// <summary>
        /// NUnit marks a [TestCase] row as not runnable when it gives fewer values than the method has parameters,
        /// or when the method returns a value with no expected result. A property generates the other parameters
        /// and checks the return value itself, so each such row of a property method runs.
        /// </summary>
        public static void AcceptPartialRows(ITest suite)
        {
            if (suite is not ParameterizedMethodSuite || suite.Method == null)
            {
                return;
            }

            var parameterCount = suite.Method.GetParameters().Length;
            foreach (var test in suite.Tests.OfType<TestMethod>())
            {
                // A row whose source failed has no arguments, and stays not runnable.
                if (test.RunState == RunState.NotRunnable &&
                    test.Arguments.Length > 0 &&
                    test.Arguments.Length <= parameterCount &&
                    test.Method.IsPublic &&
                    !test.Method.IsAbstract)
                {
                    test.RunState = RunState.Runnable;
                    test.Properties[PropertyNames.SkipReason].Clear();
                }
            }
        }

        /// <summary>
        /// Runs the body of a test in a context of its own, so that an assertion of NUnit inside the test function
        /// does not record a failure for each test case. Then records one result.
        /// </summary>
        public static TestResult Execute(TestExecutionContext context, Func<object?, Task> body)
        {
            var testObject = context.TestObject;
            Exception? failure = null;
            using (new TestExecutionContext.IsolatedContext())
            {
                try
                {
                    Task.Run(() => body(testObject)).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            }

            if (failure == null)
            {
                context.CurrentResult.SetResult(ResultState.Success);
            }
            else
            {
                context.CurrentResult.RecordException(failure);
            }

            return context.CurrentResult;
        }

        /// <summary>The arguments of a parameterized fixture, for example 5 of <c>[TestFixture(5)]</c>, or null.</summary>
        public static object?[]? FixtureArguments(Test test)
        {
            for (var parent = test.Parent; parent != null; parent = parent.Parent)
            {
                if (parent is TestFixture fixture)
                {
                    return fixture.Arguments is { Length: > 0 } arguments ? arguments : null;
                }
            }

            return null;
        }

        public static Type ClassType(Test test) =>
            test.TypeInfo?.Type ?? test.Method?.MethodInfo.DeclaringType
                ?? throw new InvalidOperationException($"The test '{test.FullName}' has no class.");
    }

    /// <summary>Runs the property of a [Property] or [Sample] method in place of one call of the method.</summary>
    internal sealed class PropertyCommand : TestCommand
    {
        public PropertyCommand(Test test) : base(test)
        {
        }

        public override TestResult Execute(TestExecutionContext context) => NUnitAdapter.Execute(context, async testObject =>
        {
            var test = (TestMethod)Test;
            var testClassType = NUnitAdapter.ClassType(test);
            var arguments = test.Arguments;
            var globalConfiguration = GlobalConfiguration.GetInstance(testClassType.Assembly);

            var initialization = PropertyInitializer.InitializeWithInstance(
                testClassType,
                test.Method.MethodInfo,
                testObject,
                arguments.Length == 0 ? null : arguments,
                new DefaultPropertyFactory(),
                globalConfiguration.Properties,
                globalConfiguration.FailureMemory,
                NUnitAdapter.FixtureArguments(test));

            await initialization.Runner.Run(initialization.Parameters);
        });
    }

    /// <summary>Compares the value of each seed of a [GenSnapshot] method with the stored snapshot.</summary>
    internal sealed class GenSnapshotCommand : TestCommand
    {
        public GenSnapshotCommand(Test test) : base(test)
        {
        }

        public override TestResult Execute(TestExecutionContext context) => NUnitAdapter.Execute(context, async testObject =>
        {
            var test = (TestMethod)Test;
            var testClassType = NUnitAdapter.ClassType(test);
            var testMethod = test.Method.MethodInfo;

            var initialization = GenSnapshotInitializer.InitializeWithInstance(
                testClassType,
                testMethod,
                testObject,
                new DefaultParametersGenFactory(),
                GlobalConfiguration.GetInstance(testClassType.Assembly).GenSnapshots);

            var failures = new List<Exception>();
            for (var seedIndex = 0; seedIndex < initialization.Seeds.Count; seedIndex++)
            {
                var (exception, _) = await GenSnapshotRunner.Run(initialization, testClassType, testMethod, seedIndex);
                if (exception != null)
                {
                    failures.Add(exception);
                }
            }

            if (failures.Count == 1)
            {
                throw failures[0];
            }

            if (failures.Count > 1)
            {
                throw new AggregateException(failures);
            }
        });
    }
}
