using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Text;
using FluentAssertions;
using GalaxyCheck.Gens;
using GalaxyCheck.Tests.Slice.S6Shapes;
using Xunit;

namespace GalaxyCheck.Tests.Slice
{
    public class S6_AutomaticTests
    {
        [Fact]
        public void test_001_S4_R16_create_equals_explicit_generator_for_each_type()
        {
            var problems = new List<string>();
            // R1
            S6Support.AssertCreateEqualsExplicit(Gen.SByte(), problems);
            S6Support.AssertCreateEqualsExplicit(Gen.UInt16(), problems);
            S6Support.AssertCreateEqualsExplicit(Gen.UInt32(), problems);
            S6Support.AssertCreateEqualsExplicit(Gen.UInt64(), problems);
            S6Support.AssertCreateEqualsExplicit(Gen.IntPtr(), problems);
            S6Support.AssertCreateEqualsExplicit(Gen.UIntPtr(), problems);
            S6Support.AssertCreateEqualsExplicit(Gen.Int128(), problems);
            S6Support.AssertCreateEqualsExplicit(Gen.UInt128(), problems);
            // R4
            S6Support.AssertCreateEqualsExplicit(Gen.BigInteger(), problems);
            // R5, R10
            S6Support.AssertCreateEqualsExplicit(Gen.Half(), problems);
            S6Support.AssertCreateEqualsExplicit(Gen.Single(), problems);
            S6Support.AssertCreateEqualsExplicit(Gen.Double(), problems);
            S6Support.AssertCreateEqualsExplicit(Gen.Decimal(), problems);
            // R11
            S6Support.AssertCreateEqualsExplicit(Gen.TimeSpan(), problems);
            S6Support.AssertCreateEqualsExplicit(Gen.DateTimeOffset(), problems);
            S6Support.AssertCreateEqualsExplicit(Gen.DateOnly(), problems);
            S6Support.AssertCreateEqualsExplicit(Gen.TimeOnly(), problems);
            // R12, R13
            S6Support.AssertCreateEqualsExplicit(Gen.Rune(), problems);
            S6Support.AssertCreateEqualsExplicit(Gen.Complex(), problems);

            problems.Should().BeEmpty("each type must give the same values from Gen.Create<T>() and from its explicit generator: {0}", string.Join("; ", problems));
        }

        [Fact]
        public void test_001_S4_R16_dictionaries_pairs_and_tuples_are_created()
        {
            var problems = new List<string>();
            CreatesValuesOfType<Dictionary<int, string>>(problems);
            CreatesValuesOfType<IDictionary<int, string>>(problems);
            CreatesValuesOfType<IReadOnlyDictionary<int, string>>(problems);
            CreatesValuesOfType<ImmutableDictionary<int, string>>(problems);
            CreatesValuesOfType<KeyValuePair<int, string>>(problems);
            CreatesValuesOfType<(int, string)>(problems);
            CreatesValuesOfType<Tuple<int, string>>(problems);

            problems.Should().BeEmpty("each type must be created with no generator error: {0}", string.Join("; ", problems));
        }

        [Fact]
        public void test_001_S4_R17_create_double_gives_values_with_a_fraction()
        {
            var values = Gen.Create<double>().Sample(iterations: 1000, seed: 0);

            values.Should().HaveCount(1000);
            values.Should().Contain(x => double.IsFinite(x) && x % 1 != 0, "a double with a fraction part must appear");
        }

        [Fact]
        public void test_001_S4_R17_create_uint_reaches_zero_and_the_maximum()
        {
            var seen = new HashSet<uint>();
            for (var seed = 0; seed < 100; seed++)
            {
                foreach (var value in Gen.Create<uint>().Sample(iterations: 100, seed: seed))
                {
                    seen.Add(value);
                }
            }

            seen.Should().Contain(0u);
            seen.Should().Contain(uint.MaxValue);
        }

        [Property]
        public void test_001_S4_R16_property_parameters_are_generated(double number, Dictionary<int, string> dictionary)
        {
            dictionary.Should().NotBeNull("the property parameter of type Dictionary<int, string> must be generated");
            dictionary.Keys.Should().OnlyHaveUniqueItems();
            _ = number;
        }

        [Fact]
        public void test_001_S4_R31_collections_are_created()
        {
            var problems = new List<string>();
            CreatesValuesOfType<Queue<int>>(problems);
            CreatesValuesOfType<Stack<int>>(problems);
            CreatesValuesOfType<SortedSet<int>>(problems);
            CreatesValuesOfType<LinkedList<int>>(problems);
            CreatesValuesOfType<ImmutableArray<int>>(problems);
            CreatesValuesOfType<ReadOnlyCollection<int>>(problems);
            CreatesValuesOfType<Memory<int>>(problems);
            CreatesValuesOfType<ReadOnlyMemory<int>>(problems);

            problems.Should().BeEmpty("each type must be created with no generator error: {0}", string.Join("; ", problems));
        }

        [Fact]
        public void test_001_S4_R31_queue_shrinks_to_three_zeros()
        {
            var result = Property.ForAll(Gen.Create<Queue<int>>(), queue => queue.Count < 3).Check(iterations: 100, seed: 0);

            result.Falsified.Should().BeTrue();
            result.Counterexample!.Value.ToArray().Should().Equal(0, 0, 0);
        }

        [Fact]
        public void test_001_S4_R32_interface_gives_each_concrete_type()
        {
            var values = Gen.Create<IShape>().Sample(iterations: 100, seed: 0);

            values.Should().HaveCount(100);
            values.OfType<Circle>().Should().NotBeEmpty("a Circle must appear");
            values.OfType<Square>().Should().NotBeEmpty("a Square must appear");
        }

        [Fact]
        public void test_001_S4_R32_abstract_class_with_no_concrete_type_is_a_generator_error()
        {
            var action = () => Gen.Create<LonelyBase>().Sample(iterations: 1, seed: 0);

            action.Should().Throw<Exceptions.GenErrorException>()
                .Which.Message.Should().Contain(typeof(LonelyBase).FullName!);
        }

        [Fact]
        public void test_001_S4_R32_registered_candidates_limit_the_interface()
        {
            var gen = Gen.Factory().RegisterImplementations<IShape>(typeof(Circle)).Create<IShape>();

            var values = gen.Sample(iterations: 100, seed: 0);

            values.Should().HaveCount(100);
            values.Should().OnlyContain(shape => shape is Circle);
        }

        private static void CreatesValuesOfType<T>(List<string> problems)
        {
            try
            {
                var values = Gen.Create<T>().Sample(iterations: 100, seed: 0);
                if (values.Count != 100)
                {
                    problems.Add($"{typeof(T).Name}: expected 100 values, got {values.Count}");
                    return;
                }

                foreach (var value in values)
                {
                    object? boxed = value;
                    if (boxed is null)
                    {
                        problems.Add($"{typeof(T).Name}: a value is null");
                        return;
                    }

                    if (boxed is not T)
                    {
                        problems.Add($"{typeof(T).Name}: a value has the type {boxed.GetType().Name}");
                        return;
                    }

                    if (!typeof(T).IsInterface && boxed.GetType() != typeof(T))
                    {
                        problems.Add($"{typeof(T).Name}: a value has the type {boxed.GetType().Name}");
                        return;
                    }

                    if (boxed is ImmutableArray<int> array && array.IsDefault)
                    {
                        problems.Add($"{typeof(T).Name}: a value is the default array");
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                problems.Add($"{typeof(T).Name}: {e.GetType().Name}: {e.Message}");
            }
        }
    }
}

namespace GalaxyCheck.Tests.Slice.S6Shapes
{
    public interface IShape
    {
    }

    public sealed record Circle(int Radius) : IShape;

    public sealed record Square(int Side) : IShape;

    public abstract class LonelyBase
    {
        public abstract int Size { get; }
    }
}
