using System;

namespace GalaxyCheck
{
    public static partial class Extensions
    {
        /// <summary>Gives the label to each test case for which the function is true.</summary>
        public static Property<T> Label<T>(this Property<T> property, string label, Func<T, bool> when) => throw new System.NotImplementedException("not implemented");

        /// <summary>Gives the label, and requires a minimum share of test cases with it.</summary>
        public static Property<T> Cover<T>(this Property<T> property, string label, double minPercent, Func<T, bool> when) => throw new System.NotImplementedException("not implemented");

        public static AsyncProperty<T> Label<T>(this AsyncProperty<T> property, string label, Func<T, bool> when) => throw new System.NotImplementedException("not implemented");

        public static AsyncProperty<T> Cover<T>(this AsyncProperty<T> property, string label, double minPercent, Func<T, bool> when) => throw new System.NotImplementedException("not implemented");
    }
}
