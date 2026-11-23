namespace GalaxyCheck
{
    using GalaxyCheck.Gens;
    using System.Linq;

    public static partial class Gen
    {
        /// <summary>
        /// Creates a generator which chooses between values of the source generators at a probability defined by their
        /// given weights.
        /// </summary>
        /// <param name="weightedGens">The source generators to choose between, with their respective weights. If all
        /// source generators have the same weight, then they are equiprobable.</param>
        /// <returns>A new generator sourced from the given generators.</returns>
        public static IGen<T> Choose<T>(params (IGen<T> gen, int weight)[] weightedGens)
        {
            IChooseGen<T> initial = new ChooseGen<T>();
            return weightedGens.Aggregate(initial, (acc, curr) => acc.WithChoice(curr.gen, curr.weight));
        }

        /// <summary>
        /// Creates a generator which chooses between values of the source generators at an equal probability.
        /// </summary>
        /// <param name="gens">The source generators to choose from.</param>
        /// <returns>A new generator sourced from the given generators.</returns>
        public static IGen<T> Choose<T>(params IGen<T>[] gens) => Choose(gens.Select(g => (g, 1)).ToArray());

        /// <summary>
        /// Creates an empty generator of choices. The generator should eventually be defined using the builder
        /// methods available on <see cref="IChooseGen{T}"/>.
        /// </summary>
        /// <returns>A new generator, which should eventually be sourced other generators.</returns>
        public static IChooseGen<T> Choose<T>() => new ChooseGen<T>();
    }

    public static partial class Extensions
    {
        /// <summary>
        /// Adds a source generator as a new choice for this <see cref="IChooseGen{T}"/>. All generators added using
        /// this method are assigned the same weight, making this is a convenient way to build an choosing generator
        /// from equiprobably source generators.
        /// </summary>
        /// <param name="gen">The generator to add the choice to.</param>
        /// <param name="alternative">The source generator to add as an choice.</param>
        /// <returns>A new choosing generator with the choice added.</returns>
        public static IChooseGen<T> WithChoice<T>(this IChooseGen<T> gen, IGen<T> alternative) =>
            gen.WithChoice(alternative, 1);
    }
}

namespace GalaxyCheck.Gens
{
    using GalaxyCheck.Gens.Internal;
    using GalaxyCheck.Gens.Parameters;
    using System.Collections.Immutable;
    using System.Linq;

    /// <summary>A generator that picks one of its weighted choices for each value.</summary>
    public interface IChooseGen<T> : IGen<T>
    {
        /// <summary>Adds a choice with a weight. A choice with a higher weight is picked more often.</summary>
        IChooseGen<T> WithChoice(IGen<T> gen, int weight);
    }

    internal record ChooseGen<T>(ImmutableList<ChooseGen<T>.Choice> Choices) : GenProvider<T>, IChooseGen<T>
    {
        public record Choice(IGen<T> Gen, int Weight);

        public ChooseGen() : this(ImmutableList.Create<Choice>()) { }

        public IChooseGen<T> WithChoice(IGen<T> gen, int weight) => this with
        {
            Choices = Choices.Add(new Choice(gen, weight))
        };

        protected override IGen<T> Get => CreateGen(Choices);

        private static IGen<T> CreateGen(ImmutableList<Choice> choices)
        {
            if (choices.Any() == false)
            {
                return Gen.Advanced.Error<T>("'choices' must be non-empty");
            }

            if (choices.Any(choice => choice.Weight < 0))
            {
                return Gen.Advanced.Error<T>("'choices' must not contain a negatively weighted generator");
            }

            if (choices.Sum(choice => choice.Weight) == 0)
            {
                return Gen.Advanced.Error<T>("'choices' must contain at least one generator with a weight greater than zero");
            }

            if (choices.Count == 1)
            {
                return choices[0].Gen;
            }

            return new WeightedChoiceGen<T>(
                choices.Select(c => c.Gen.Advanced).ToArray(),
                choices.Select(c => c.Weight).ToArray());
        }
    }

    /// <summary>
    /// Picks one generator by weight. The choice is the index of the generator, so a value shrinks towards the
    /// value of the first generator.
    /// </summary>
    internal sealed record WeightedChoiceGen<T>(IGenAdvanced<T>[] Gens, int[] Weights) : BaseGen<T>
    {
        private readonly long _totalWeight = Weights.Sum(w => (long)w);

        public override T Generate(GenSource source)
        {
            // The branch and its value are a span, so that the shrinker can replace a branch by a branch inside it.
            var start = source.BeginSpan();
            int index;
            if (source.IsReplaying)
            {
                index = (int)source.Read((ulong)(Gens.Length - 1));
                while (Weights[index] == 0)
                {
                    index = (index + 1) % Gens.Length;
                }
            }
            else
            {
                var pick = (long)source.NextBounded((ulong)(_totalWeight - 1));
                index = 0;
                while (pick >= Weights[index])
                {
                    pick -= Weights[index];
                    index++;
                }

                source.Record((ulong)index);
            }

            var value = Gens[index].Generate(source);
            source.EndSpan(start);
            return value;
        }
    }
}
