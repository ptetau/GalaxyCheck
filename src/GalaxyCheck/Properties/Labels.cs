using GalaxyCheck.Properties;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GalaxyCheck
{
    public static partial class Extensions
    {
        /// <summary>Gives the label to each test case for which the function is true.</summary>
        public static Property<T> Label<T>(this Property<T> property, string label, Func<T, bool> when) =>
            new Property<T>(property, LabelRules.Add(property.Rules, label, null, when));

        /// <summary>Gives the label, and requires a minimum share of test cases with it.</summary>
        public static Property<T> Cover<T>(this Property<T> property, string label, double minPercent, Func<T, bool> when) =>
            new Property<T>(property, LabelRules.Add(property.Rules, label, minPercent, when));

        /// <summary>Counts the share of test cases for which <paramref name="when"/> is true under a label.</summary>
        public static AsyncProperty<T> Label<T>(this AsyncProperty<T> property, string label, Func<T, bool> when) =>
            new AsyncProperty<T>(property, LabelRules.Add(property.Rules, label, null, when));

        /// <summary>Fails the run when the share of test cases with the label is below <paramref name="minPercent"/>.</summary>
        public static AsyncProperty<T> Cover<T>(this AsyncProperty<T> property, string label, double minPercent, Func<T, bool> when) =>
            new AsyncProperty<T>(property, LabelRules.Add(property.Rules, label, minPercent, when));
    }
}

namespace GalaxyCheck.Properties
{
    internal static class LabelRules
    {
        public static IReadOnlyList<LabelRule<T>> Add<T>(
            IReadOnlyList<LabelRule<T>>? rules,
            string label,
            double? minPercent,
            Func<T, bool> when)
        {
            if (label == null) throw new ArgumentNullException(nameof(label));
            if (when == null) throw new ArgumentNullException(nameof(when));
            if (minPercent != null && (double.IsNaN(minPercent.Value) || minPercent.Value < 0 || minPercent.Value > 100))
            {
                throw new ArgumentOutOfRangeException(nameof(minPercent), "The required share must be from 0 to 100.");
            }

            var list = rules == null ? new List<LabelRule<T>>() : new List<LabelRule<T>>(rules);
            list.Add(new LabelRule<T>(label, minPercent, when));
            return list;
        }

        /// <summary>The rules of a property whose input is wrapped in an array (the conversion to Property).</summary>
        public static IReadOnlyList<LabelRule<object>>? Box<T>(IReadOnlyList<LabelRule<T>>? rules) =>
            rules?.Select(rule => new LabelRule<object>(
                rule.Label,
                rule.MinPercent,
                input => rule.When((T)((object?[])input)[0]!))).ToList();
    }
}
