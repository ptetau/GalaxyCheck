using System;

namespace GalaxyCheck
{
    /// <summary>Thrown by a [Sample] test to list the sampled inputs.</summary>
#if GALAXYCHECK_XUNIT2
    public
#else
    internal
#endif
    class SampleException : Exception
    {
        /// <summary>Creates the exception with the list of sampled inputs.</summary>
        public SampleException(string message) : base($"Test case failed to prevent false-positives.{Environment.NewLine}{Environment.NewLine}{message}") { }
    }
}
