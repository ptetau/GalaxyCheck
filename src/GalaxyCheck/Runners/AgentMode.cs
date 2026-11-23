using System;
using System.Globalization;

namespace GalaxyCheck.Runners
{
    /// <summary>
    /// The settings that the environment variables give to a run: <c>GALAXYCHECK_AGENT=1</c> turns on the quiet mode
    /// for an agent, and <c>GALAXYCHECK_SEED=n</c> sets the seed of each run with no seed option.
    /// </summary>
    internal static class AgentMode
    {
        public const string AgentVariable = "GALAXYCHECK_AGENT";
        public const string SeedVariable = "GALAXYCHECK_SEED";

        /// <summary>The most lines of a failure message in the quiet mode.</summary>
        public const int MaxMessageLines = 40;

        /// <summary>True when <c>GALAXYCHECK_AGENT</c> is <c>1</c> or <c>true</c>.</summary>
        public static bool IsOn
        {
            get
            {
                var value = Read(AgentVariable);
                return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// The seed of a run with no seed option: the value of <c>GALAXYCHECK_SEED</c>, else 0 in the quiet mode,
        /// else null. A value that is not an integer is ignored.
        /// </summary>
        public static int? DefaultSeed
        {
            get
            {
                var seedText = Read(SeedVariable);
                if (seedText != null &&
                    int.TryParse(seedText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var seed))
                {
                    return seed;
                }

                return IsOn ? 0 : null;
            }
        }

        private static string? Read(string name)
        {
            try
            {
                return Environment.GetEnvironmentVariable(name);
            }
            catch (System.Security.SecurityException)
            {
                return null;
            }
        }
    }
}
