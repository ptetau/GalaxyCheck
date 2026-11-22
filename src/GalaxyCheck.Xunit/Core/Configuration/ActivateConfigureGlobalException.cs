using System;

namespace GalaxyCheck.Configuration
{
#if GALAXYCHECK_XUNIT2
    public
#else
    internal
#endif
    class ActivateConfigureGlobalException : Exception
    {
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
