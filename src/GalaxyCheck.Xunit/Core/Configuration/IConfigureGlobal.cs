namespace GalaxyCheck.Configuration
{
    /// <summary>Implement it in a test assembly to set the global configuration.</summary>
    public interface IConfigureGlobal
    {
        /// <summary>Sets the global configuration. GalaxyCheck calls it one time for each test assembly.</summary>
        void Configure(IGlobalConfiguration instance);
    }
}
