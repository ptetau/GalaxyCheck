using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace GalaxyCheck.Internal.Infrastructure.Xunit
{
    /// <summary>Finds the test cases of a [GenSnapshot] method for xunit.</summary>
    public class GenSnapshotDiscoverer : IXunitTestCaseDiscoverer
    {
        private readonly IMessageSink _messageSink;

        /// <summary>Creates the discoverer.</summary>
        public GenSnapshotDiscoverer(IMessageSink messageSink)
        {
            _messageSink = messageSink;
        }

        /// <summary>Returns the one test case of the method.</summary>
        public IEnumerable<IXunitTestCase> Discover(
            ITestFrameworkDiscoveryOptions discoveryOptions,
            ITestMethod testMethod,
            IAttributeInfo factAttribute)
        {
            yield return new GenSnapshotTestCase(
                _messageSink,
                discoveryOptions.MethodDisplayOrDefault(),
                discoveryOptions.MethodDisplayOptionsOrDefault(),
                testMethod);
        }
    }
}
