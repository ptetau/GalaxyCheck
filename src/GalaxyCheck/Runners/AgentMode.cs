using System;
using System.Globalization;
using Verifiable.Contracts;

namespace GalaxyCheck.Runners
{
    [Verifiable]
    internal readonly record struct AgentSettings(bool IsAgent, int? DefaultSeed);

    [Verifiable]
    internal readonly record struct AgentSeedParseError(string RawValue);

    [Verifiable]
    internal static class AgentModeParser
    {
        public static bool IsAgentEnabled(string? agentValue) =>
            agentValue == "1" || agentValue == "true" || agentValue == "TRUE" || agentValue == "True";

        public static Result<AgentSettings, AgentSeedParseError> Resolve(
            bool isAgent,
            bool hasSeedText,
            string? rawSeedText,
            bool parsedOk,
            int parsedSeed)
        {
            Spec.Ensures<Result<AgentSettings, AgentSeedParseError>>(
                r => hasSeedText
                    ? (parsedOk ? (r.IsSuccess && r.Value.DefaultSeed == parsedSeed) : !r.IsSuccess)
                    : (r.IsSuccess && (isAgent ? r.Value.DefaultSeed == 0 : r.Value.DefaultSeed == null)),
                "Agent mode defaults seed to 0 when no seed variable is set, uses parsedSeed when valid, and fails when non-empty seed text is invalid.");

            if (hasSeedText)
            {
                if (parsedOk)
                {
                    return Result<AgentSettings, AgentSeedParseError>.Ok(new AgentSettings(isAgent, parsedSeed));
                }

                return Result<AgentSettings, AgentSeedParseError>.Fail(new AgentSeedParseError(rawSeedText ?? string.Empty));
            }

            return Result<AgentSettings, AgentSeedParseError>.Ok(new AgentSettings(isAgent, isAgent ? 0 : null));
        }
    }

    /// <summary>
    /// The settings that the environment variables give to a run: <c>GALAXYCHECK_AGENT=1</c> turns on the quiet mode
    /// for an agent, and <c>GALAXYCHECK_SEED=n</c> sets the seed of each run with no seed option.
    /// </summary>
    [Unverifiable]
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
        /// else null.
        /// </summary>
        /// <exception cref="Exceptions.GenErrorException">The value of <c>GALAXYCHECK_SEED</c> is not an integer.</exception>
        public static int? DefaultSeed
        {
            get
            {
                var seedText = Read(SeedVariable);
                var hasSeed = !string.IsNullOrWhiteSpace(seedText);
                var parsedSeed = 0;
                var parsedOk = hasSeed && int.TryParse(seedText!.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedSeed);
                var resolved = AgentModeParser.Resolve(IsOn, hasSeed, seedText, parsedOk, parsedSeed);
                if (resolved.IsSuccess)
                {
                    return resolved.Value.DefaultSeed;
                }

                // A seed that is ignored gives other values on each run, while the agent believes that it is fixed.
                throw new Exceptions.GenErrorException(
                    $"Error: the environment variable {SeedVariable} has the value '{resolved.Error.RawValue}', which is not an integer from {int.MinValue} to {int.MaxValue}." +
                    Environment.NewLine +
                    $"Fix: set {SeedVariable} to an integer, for example {SeedVariable}=7, or unset it.",
                    null);
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
