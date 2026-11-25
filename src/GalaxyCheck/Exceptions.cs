using Verifiable.Contracts;
using System;
using GalaxyCheck.Gens.Parameters;

namespace GalaxyCheck
{
    /// <summary>The exceptions that GalaxyCheck throws.</summary>
    [Unverifiable]
    public static class Exceptions
    {
        /// <summary>Thrown when a generator cannot generate, for example when its options conflict. The last line of the message starts with <c>Fix: </c>.</summary>
        public class GenErrorException : Exception
        {
            /// <summary>The seed and size of the test case that hit the error, or null when no test case ran.</summary>
            public GenParameters? ReplayParameters { get; }

            /// <summary>Creates the error. A message whose last line is not a fix line gets one.</summary>
            public GenErrorException(string message, GenParameters? replayParameters) : base(Internal.FixHint.WithFix(message))
            {
                ReplayParameters = replayParameters;
            }
        }

        /// <summary>Thrown when a generator discards too many values to give one.</summary>
        public class GenExhaustionException : Exception
        {
            /// <summary>Creates the exception with the default message.</summary>
            public GenExhaustionException()
            {
            }

            /// <summary>Creates the exception with a message.</summary>
            public GenExhaustionException(string message) : base(message)
            {
            }
        }

        /// <summary>Thrown by Minimum when no value passes the predicate.</summary>
        public class NoMinimalFoundException : Exception
        {
            /// <summary>Creates the exception with the default message.</summary>
            public NoMinimalFoundException()
            {
            }
        }

        /// <summary>Thrown when a generator passes a built-in safety limit, for example the iteration limit of an infinite enumerable.</summary>
        public class GenLimitExceededException : Exception
        {
            /// <summary>Creates the exception with a message.</summary>
            public GenLimitExceededException(string message)
                : base(message)
            {
            }
        }
    }
}
