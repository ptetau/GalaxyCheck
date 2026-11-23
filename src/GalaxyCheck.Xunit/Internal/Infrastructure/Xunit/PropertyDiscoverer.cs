using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace GalaxyCheck.Internal.Infrastructure.Xunit
{
    /// <summary>Finds the test cases of a [Property] method for xunit.</summary>
    public class PropertyDiscoverer : TheoryDiscoverer
    {
        private readonly IMessageSink _messageSink;

        /// <summary>The count of data rows that the discovery of the current method has seen, in the declared order.</summary>
        [System.ThreadStatic]
        private static int _rowCount;

        /// <summary>Creates the discoverer.</summary>
        public PropertyDiscoverer(IMessageSink messageSink) : base(messageSink)
        {
            _messageSink = messageSink;
        }

        /// <summary>Returns the test cases of the method, one for each data row.</summary>
        public override IEnumerable<IXunitTestCase> Discover(
            ITestFrameworkDiscoveryOptions discoveryOptions,
            ITestMethod testMethod,
            IAttributeInfo factAttribute)
        {
            if (testMethod.Method.GetCustomAttributes(typeof(DataAttribute)).Any())
            {
                // The base reads the rows in the declared order, in this call, so the count gives each row its index.
                _rowCount = 0;
                var testCases = base.Discover(discoveryOptions, testMethod, factAttribute).ToList();
                foreach (var testCase in testCases)
                {
                    yield return testCase;
                }
            }
            else
            {
                yield return new PropertyTestCase(
                    _messageSink,
                    discoveryOptions.MethodDisplayOrDefault(),
                    discoveryOptions.MethodDisplayOptionsOrDefault(),
                    testMethod,
                    null);
            }
        }

        /// <summary>Returns the test case of one data row.</summary>
        protected override IEnumerable<IXunitTestCase> CreateTestCasesForDataRow(
            ITestFrameworkDiscoveryOptions discoveryOptions,
            ITestMethod testMethod,
            IAttributeInfo theoryAttribute,
            object[] dataRow)
        {
            yield return new PropertyTestCase(
                _messageSink,
                discoveryOptions.MethodDisplayOrDefault(),
                discoveryOptions.MethodDisplayOptionsOrDefault(),
                testMethod,
                dataRow,
                (_rowCount++).ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}
