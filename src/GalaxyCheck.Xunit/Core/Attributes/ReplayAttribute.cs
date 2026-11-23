using System;

namespace GalaxyCheck
{
    /// <summary>Runs the one test case of a replay string from a failure message.</summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ReplayAttribute : Attribute
    {
        /// <summary>The replay string.</summary>
        public string Replay { get; }

        /// <summary>Creates the attribute with a replay string.</summary>
        public ReplayAttribute(string replay)
        {
            Replay = replay;
        }
    }
}
