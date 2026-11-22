using GalaxyCheck.Properties;
using System.Collections.Generic;

namespace GalaxyCheck
{
    public partial class Property : Property<object>
    {
        public Property(IGen<Test<object>> gen) : base(gen)
        {
        }

        public Property(IGen<Test> gen) : this(gen.Select(t => t.Cast<object>()))
        {
        }

        internal Property(IGen<Test<object>> gen, IReadOnlyList<LabelRule<object>>? rules) : base(gen, rules)
        {
        }

        internal Property(IGen<Test> gen, IReadOnlyList<LabelRule<object>>? rules) : this(gen.Select(t => t.Cast<object>()), rules)
        {
        }
    }

    public class Property<T> : IGen<Property.Test<T>>
    {
        private readonly IGen<Property.Test<T>> _gen;

        public Property(IGen<Property.Test<T>> gen)
        {
            _gen = gen;
        }

        internal Property(IGen<Property.Test<T>> gen, IReadOnlyList<LabelRule<T>>? rules)
        {
            _gen = gen;
            Rules = rules;
        }

        /// <summary>The labels and cover rules of the property.</summary>
        internal IReadOnlyList<LabelRule<T>>? Rules { get; }

        public IGenAdvanced<Property.Test<T>> Advanced => _gen.Advanced;

        IGenAdvanced IGen.Advanced => Advanced;

        public static implicit operator Property(Property<T> property) => new Property(
            from test in property
            select TestFactory.Create(new object[] { test.Input }, test.Output, test.PresentedInput),
            LabelRules.Box(property.Rules));
    }

    public class AsyncProperty : AsyncProperty<object>
    {
        public AsyncProperty(IGen<Property.AsyncTest<object>> gen) : base(gen)
        {
        }

        public AsyncProperty(IGen<Property.AsyncTest> gen) : this(gen.Select(t => t.Cast<object>()))
        {
        }

        internal AsyncProperty(IGen<Property.AsyncTest<object>> gen, IReadOnlyList<LabelRule<object>>? rules) : base(gen, rules)
        {
        }

        internal AsyncProperty(IGen<Property.AsyncTest> gen, IReadOnlyList<LabelRule<object>>? rules) : this(gen.Select(t => t.Cast<object>()), rules)
        {
        }
    }

    public class AsyncProperty<T> : IGen<Property.AsyncTest<T>>
    {
        private readonly IGen<Property.AsyncTest<T>> _gen;

        public AsyncProperty(IGen<Property.AsyncTest<T>> gen)
        {
            _gen = gen;
        }

        internal AsyncProperty(IGen<Property.AsyncTest<T>> gen, IReadOnlyList<LabelRule<T>>? rules)
        {
            _gen = gen;
            Rules = rules;
        }

        /// <summary>The labels and cover rules of the property.</summary>
        internal IReadOnlyList<LabelRule<T>>? Rules { get; }

        public IGenAdvanced<Property.AsyncTest<T>> Advanced => _gen.Advanced;

        IGenAdvanced IGen.Advanced => Advanced;

        public static implicit operator AsyncProperty(AsyncProperty<T> property) => new AsyncProperty(
            from test in property
            select TestFactory.Create(new object[] { test.Input }, test.Output, test.PresentedInput),
            LabelRules.Box(property.Rules));
    }
}
