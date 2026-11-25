using GalaxyCheck.Gens.Parameters;
using System;

namespace GalaxyCheck.Gens.Internal
{
    /// <summary>
    /// The base of each generator that makes its choices directly from the source.
    /// </summary>
    [global::Verifiable.Contracts.Unverifiable]
    internal abstract record BaseGen<T> : IGen<T>, IGenAdvanced<T>
    {
        public IGenAdvanced<T> Advanced => this;

        IGenAdvanced IGen.Advanced => this;

        public abstract T Generate(GenSource source);

        object? IGenAdvanced.GenerateObject(GenSource source) => Generate(source);
    }

    /// <summary>
    /// A generator that is defined by another generator. The other generator is built once, on the first use.
    /// </summary>
    [global::Verifiable.Contracts.Unverifiable]
    internal abstract record GenProvider<T> : IGen<T>, IGenAdvanced<T>, GalaxyCheck.Properties.IFiniteGen
    {
        // A record copy (with) copies these fields, so the owner reference tells whether the cache belongs to this
        // instance.
        private Cache? _cache;

        private sealed class Cache
        {
            public Cache(object owner, IGenAdvanced<T> gen)
            {
                Owner = owner;
                Gen = gen;
            }

            public object Owner { get; }

            public IGenAdvanced<T> Gen { get; }
        }

        public IGenAdvanced<T> Advanced => this;

        IGenAdvanced IGen.Advanced => this;

        protected abstract IGen<T> Get { get; }

        public T Generate(GenSource source) => Resolve().Generate(source);

        object? IGenAdvanced.GenerateObject(GenSource source) => Generate(source);

        /// <summary>The generator is finite when the generator that defines it is finite.</summary>
        public virtual long? Count(long limit) => (Resolve() as GalaxyCheck.Properties.IFiniteGen)?.Count(limit);

        public virtual void WriteChoices(long index, System.Collections.Generic.List<ulong> choices) =>
            ((GalaxyCheck.Properties.IFiniteGen)Resolve()).WriteChoices(index, choices);

        private IGenAdvanced<T> Resolve()
        {
            var cache = _cache;
            if (cache is null || !ReferenceEquals(cache.Owner, this))
            {
                cache = new Cache(this, Get.Advanced);
                _cache = cache;
            }

            return cache.Gen;
        }
    }

    /// <summary>
    /// A generator that runs a function on the source.
    /// </summary>
    [global::Verifiable.Contracts.Unverifiable]
    internal sealed record FunctionGen<T>(Func<GenSource, T> Func) : BaseGen<T>
    {
        public override T Generate(GenSource source) => Func(source);
    }

    /// <summary>
    /// Thrown by a generator to report a generator error. The runner turns it into a GenErrorException.
    /// </summary>
    [global::Verifiable.Contracts.Unverifiable]
    internal sealed class GenErrorSignal : Exception
    {
        public GenErrorSignal(string message) : base(message)
        {
        }
    }
}
