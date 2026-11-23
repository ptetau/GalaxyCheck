using System;

namespace GalaxyCheck
{
    /// <summary>Takes the generator of a parameter from a static property or field of the test class.</summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class MemberGenAttribute : Attribute
    {
        /// <summary>The name of the member that holds the generator.</summary>
        public string MemberName { get; }

        /// <summary>Names the member that holds the generator, for example <c>nameof(MyGen)</c>.</summary>
        public MemberGenAttribute(string memberName)
        {
            MemberName = memberName;
        }
    }
}
