using System;

namespace GalaxyCheck
{
#if GALAXYCHECK_XUNIT2
    public
#else
    internal
#endif
    class SampleException : Exception
    {
        public SampleException(string message) : base($"Test case failed to prevent false-positives.{Environment.NewLine}{Environment.NewLine}{message}") { }
    }
}
