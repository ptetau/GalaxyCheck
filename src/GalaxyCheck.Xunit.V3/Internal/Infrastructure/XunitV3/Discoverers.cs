using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;
using static GalaxyCheck.Internal.Infrastructure.XunitV3.DiscovererHelpers;

namespace GalaxyCheck.Internal.Infrastructure.XunitV3
{
    /// <summary>
    /// Finds one test for a [Property] or [Sample] method, or one test for each data row of its data attributes
    /// ([InlineData], [MemberData], [ClassData]).
    /// </summary>
    internal sealed class PropertyDiscoverer : TheoryDiscoverer
    {
        public override ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(
            ITestFrameworkDiscoveryOptions discoveryOptions,
            IXunitTestMethod testMethod,
            IFactAttribute factAttribute)
        {
            if (testMethod.DataAttributes.Count > 0)
            {
                return base.Discover(discoveryOptions, testMethod, factAttribute);
            }

            var details = TestIntrospectionHelper.GetTestCaseDetails(discoveryOptions, testMethod, factAttribute);

            IXunitTestCase testCase = new PropertyTestCase(
                details.ResolvedTestMethod,
                details.TestCaseDisplayName,
                details.UniqueID,
                details.Explicit,
                null,
                false,
                details.SkipExceptions,
                details.SkipReason,
                details.SkipType,
                details.SkipUnless,
                details.SkipWhen,
                CopyTraits(testMethod.Traits),
                null,
                details.SourceFilePath,
                details.SourceLineNumber,
                details.Timeout);

            return new([testCase]);
        }

        protected override ValueTask<IReadOnlyCollection<IXunitTestCase>> CreateTestCasesForDataRow(
            ITestFrameworkDiscoveryOptions discoveryOptions,
            IXunitTestMethod testMethod,
            ITheoryAttribute theoryAttribute,
            ITheoryDataRow dataRow,
            object?[] testMethodArguments,
            string? index)
        {
            var details = TestIntrospectionHelper.GetTestCaseDetailsForTheoryDataRow(discoveryOptions, testMethod, theoryAttribute, dataRow, testMethodArguments, index);

            IXunitTestCase testCase = new PropertyTestCase(
                details.ResolvedTestMethod,
                details.TestCaseDisplayName,
                details.UniqueID,
                details.Explicit,
                dataRow.Label,
                dataRow.DisableParallelization ?? false,
                details.SkipExceptions,
                details.SkipReason,
                details.SkipType,
                details.SkipUnless,
                details.SkipWhen,
                TestIntrospectionHelper.GetTraits(testMethod, dataRow),
                testMethodArguments,
                details.SourceFilePath,
                details.SourceLineNumber,
                details.Timeout);

            return new([testCase]);
        }

        protected override async ValueTask<IReadOnlyCollection<IXunitTestCase>> CreateTestCasesForTheory(
            ITestFrameworkDiscoveryOptions discoveryOptions,
            IXunitTestMethod testMethod,
            ITheoryAttribute theoryAttribute)
        {
            var testCases = await base.CreateTestCasesForTheory(discoveryOptions, testMethod, theoryAttribute);

            var result = new List<IXunitTestCase>();
            foreach (var testCase in testCases)
            {
                if (testCase is XunitDelayEnumeratedTheoryTestCase delayEnumerated)
                {
                    result.Add(new PropertyDelayEnumeratedTestCase(
                        delayEnumerated.TestMethod,
                        delayEnumerated.TestCaseDisplayName,
                        delayEnumerated.UniqueID,
                        delayEnumerated.Explicit,
                        delayEnumerated.SkipTestWithoutData,
                        delayEnumerated.SkipExceptions,
                        delayEnumerated.SkipReason,
                        delayEnumerated.SkipType,
                        delayEnumerated.SkipUnless,
                        delayEnumerated.SkipWhen,
                        delayEnumerated.Traits,
                        delayEnumerated.SourceFilePath,
                        delayEnumerated.SourceLineNumber,
                        delayEnumerated.Timeout));
                }
                else
                {
                    result.Add(testCase);
                }
            }

            return result;
        }
    }

    internal static class DiscovererHelpers
    {
        public static Dictionary<string, HashSet<string>> CopyTraits(IReadOnlyDictionary<string, IReadOnlyCollection<string>> traits)
        {
            var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var trait in traits)
            {
                result[trait.Key] = new HashSet<string>(trait.Value, StringComparer.OrdinalIgnoreCase);
            }

            return result;
        }
    }

    /// <summary>Finds one test for a [GenSnapshot] method.</summary>
    internal sealed class GenSnapshotDiscoverer : IXunitTestCaseDiscoverer
    {
        public ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(
            ITestFrameworkDiscoveryOptions discoveryOptions,
            IXunitTestMethod testMethod,
            IFactAttribute factAttribute)
        {
            var details = TestIntrospectionHelper.GetTestCaseDetails(discoveryOptions, testMethod, factAttribute);

            IXunitTestCase testCase = new GenSnapshotTestCase(
                details.ResolvedTestMethod,
                details.TestCaseDisplayName,
                details.UniqueID,
                details.Explicit,
                details.SkipExceptions,
                details.SkipReason,
                details.SkipType,
                details.SkipUnless,
                details.SkipWhen,
                CopyTraits(testMethod.Traits),
                details.SourceFilePath,
                details.SourceLineNumber,
                details.Timeout);

            return new([testCase]);
        }
    }
}
