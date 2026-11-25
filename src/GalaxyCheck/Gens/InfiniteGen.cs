namespace GalaxyCheck
{
    using GalaxyCheck.Gens;
    using System.Collections.Generic;

    public static partial class Gen
    {
        /// <summary>
        /// Creates a generator that produces infinite enumerables, the elements of which are produced by the given
        /// generator. Infinite enumerables are supported by this generator, but by default there is a limit of 1000
        /// elements in place - and the iterator will throw if that limit is exceeded. This is a pragmatic limit to
        /// avert confusion of cases where the consuming code trys to enumerate an infinite enumerable (and
        /// consequently hangs forever).
        /// </summary>
        /// <param name="elementGen">The generator used to produce the elements of the list.</param>
        /// <param name="iterationLimit">The hard iteration limit that should be applied to the generator.</param>
        /// <returns>The new generator.</returns>
        public static IGen<IEnumerable<T>> Infinite<T>(IGen<T> elementGen, int? iterationLimit = 1000) =>
            new InfiniteGen<T>(elementGen, iterationLimit);
    }

    public static partial class Extensions
    {
        /// <summary>
        /// Creates a generator that produces infinite enumerables, the elements of which are produced by the given
        /// generator. Infinite enumerables are supported by this generator, but by default there is a limit of 1000
        /// elements in place - and the iterator will throw if that limit is exceeded. This is a pragmatic limit to
        /// avert confusion of cases where the consuming code trys to enumerate an infinite enumerable (and
        /// consequently hangs forever).
        /// </summary>
        /// <param name="elementGen">The generator used to produce the elements of the list.</param>
        /// <param name="iterationLimit">The hard iteration limit that should be applied to the generator.</param>
        /// <returns>The new generator.</returns>
        public static IGen<IEnumerable<T>> InfiniteOf<T>(this IGen<T> elementGen, int? iterationLimit = 1000) =>
            Gen.Infinite(elementGen, iterationLimit);
    }
}

namespace GalaxyCheck.Gens
{
    using GalaxyCheck.Gens.Internal;
    using GalaxyCheck.Gens.Parameters;
    using System.Collections;
    using System.Collections.Generic;

    [global::Verifiable.Contracts.Unverifiable]
    internal record InfiniteGen<T> : BaseGen<IEnumerable<T>>, IGen<IEnumerable<T>>
    {
        private readonly IGen<T> _elementGen;
        private readonly int? _iterationLimit;

        public InfiniteGen(IGen<T> elementGen, int? iterationLimit)
        {
            _elementGen = elementGen;
            _iterationLimit = iterationLimit;
        }

        public InfiniteGen(IGen<T> elementGen)
            : this(elementGen, 1000)
        {
        }

        public override IEnumerable<T> Generate(GenSource source)
        {
            // The seed is for the elements that are enumerated after the test case ended. It does not shrink.
            var seed = source.DrawUniform(ulong.MaxValue);
            source.FreezeLast();
            return new InfiniteEnumerable(_elementGen.Advanced, source, source.TestCase, seed, source.Size, _iterationLimit);
        }

        /// <summary>
        /// Generates each element on its first enumeration. After its test case, the enumerable repeats the elements
        /// that the test case enumerated, so that a counterexample is the same after the test case as in it. While its test case runs, an element is a choice of the
        /// test case, so it shrinks with the other choices. Each enumeration gives the same elements.
        /// </summary>
        private sealed class InfiniteEnumerable : IEnumerable<T>
        {
            private readonly IGenAdvanced<T> _elementGen;
            private readonly GenSource _source;
            private readonly int _testCase;
            private readonly int _size;
            private readonly int? _iterationLimit;
            private readonly List<T> _elements = new();
            private readonly ulong _seed;
            private int _enumeratedInTestCase;
            private GenSource? _detachedSource;

            public InfiniteEnumerable(IGenAdvanced<T> elementGen, GenSource source, int testCase, ulong seed, int size, int? iterationLimit)
            {
                _elementGen = elementGen;
                _source = source;
                _testCase = testCase;
                _seed = seed;
                _size = size;
                _iterationLimit = iterationLimit;
            }

            public IEnumerator<T> GetEnumerator()
            {
                for (var i = 0; ; i++)
                {
                    if (_iterationLimit.HasValue && i >= _iterationLimit.Value)
                    {
                        ThrowGenLimitExceeded();
                    }

                    T element;
                    lock (_elements)
                    {
                        if (i >= _elements.Count)
                        {
                            _elements.Add(GenerateElement());
                        }

                        element = _elements[i];
                    }

                    yield return element;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private T GenerateElement()
            {
                if (_source.IsActive && _source.TestCase == _testCase)
                {
                    var start = _source.BeginSpan();
                    var element = _elementGen.Generate(_source);
                    _source.EndSpan(start);
                    _enumeratedInTestCase++;
                    return element;
                }

                if (_enumeratedInTestCase > 0)
                {
                    return _elements[_elements.Count % _enumeratedInTestCase];
                }

                if (_detachedSource is null)
                {
                    _detachedSource = new GenSource(_seed, _size, capacity: 8);
                    _detachedSource.BeginRandom(_size);
                }

                return _elementGen.Generate(_detachedSource);
            }

            private static void ThrowGenLimitExceeded()
            {
                var message = "Infinite enumerable exceeded iteration limit. This is a built-in safety mechanism to prevent hanging tests. Relax this constraint by configuring the iterationLimit parameter.";
                throw new Exceptions.GenLimitExceededException(message);
            }
        }
    }
}
