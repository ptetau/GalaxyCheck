using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GalaxyCheck.Internal
{
    internal class PropertySampleRunner : IPropertyRunner
    {
        public async Task Run(PropertyRunParameters parameters)
        {
            var log = new List<string>();

            await parameters.Property.PrintAsync(
                stdout: log.Add,
                seed: parameters.Seed,
                size: parameters.Size,
                iterations: parameters.Iterations);

            // The quiet mode keeps the message of a [Sample] test in the line limit too.
            throw new SampleException(FailureMessage.Limit(string.Join(Environment.NewLine, log)));
        }
    }
}
