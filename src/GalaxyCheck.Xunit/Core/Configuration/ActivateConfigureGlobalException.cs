using System;

namespace GalaxyCheck.Configuration
{
    /// <summary>Thrown when an IConfigureGlobal class cannot be created or throws.</summary>
#if GALAXYCHECK_XUNIT2
    public
#else
    internal
#endif
    class ActivateConfigureGlobalException : Exception
    {
        /// <summary>Creates the exception for the class and its error.</summary>
        public ActivateConfigureGlobalException(Type type, Exception ex)
            : base(BuildMessage(type, ex), ex)
        {
        }

        private static string BuildMessage(Type type, Exception ex)
        {
            return @$"Failed to create instance of type '{type}' during global GalaxyCheck configuration.

Exception: {ex}";
        }
    }
}
