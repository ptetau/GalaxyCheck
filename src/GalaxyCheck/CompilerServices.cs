namespace System.Runtime.CompilerServices
{
    /// <summary>Lets the compiler use init accessors.</summary>
    public class IsExternalInit
    {
        /// <summary>Creates the marker. The compiler uses the type, not an instance.</summary>
        public IsExternalInit()
        {
        }
    }
}