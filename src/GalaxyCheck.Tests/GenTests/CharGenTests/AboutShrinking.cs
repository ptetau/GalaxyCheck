using FluentAssertions;
using GalaxyCheck;
using System.Linq;

namespace Tests.V2.GenTests.CharGenTests
{
    /// <summary>
    /// There's a cascading system of shrinking, depending on which character sets are included. That priority is in
    /// the order laid out in these tests.
    /// </summary>
    public class AboutShrinking
    {
        [Property]
        public Property IfCharTypeHasAlphabetical_ItShrinksToLowercaseA() => new Property(
            from charType in TestGen.CharType()
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Char(GalaxyCheck.Gen.CharType.Alphabetical | charType);

                var minimum = gen.Minimum(seed: seed, size: size);

                minimum.Should().Be('a');
            }));

        [Property]
        public Property OtherwiseIfCharTypeHasWhitespace_ItShrinksToSpace() => new Property(
            from charType in TestGen.CharType(GalaxyCheck.Gen.CharType.Alphabetical)
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Char(GalaxyCheck.Gen.CharType.Whitespace | charType);

                var minimum = gen.Minimum(seed: seed, size: size);

                minimum.Should().Be(' ');
            }));

        [Property]
        public Property OtherwiseIfCharTypeHasNumeric_ItShrinksTo0() => new Property(
            from charType in TestGen.CharType(
                GalaxyCheck.Gen.CharType.Whitespace,
                GalaxyCheck.Gen.CharType.Alphabetical)
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Char(GalaxyCheck.Gen.CharType.Numeric | charType);

                var minimum = gen.Minimum(seed: seed, size: size);

                minimum.Should().Be('0');
            }));

        [Property]
        public Property OtherwiseIfCharTypeHasSymbol_ItShrinksToExclamationMark() => new Property(
            from charType in TestGen.CharType(
                GalaxyCheck.Gen.CharType.Whitespace,
                GalaxyCheck.Gen.CharType.Alphabetical,
                GalaxyCheck.Gen.CharType.Numeric)
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Char(GalaxyCheck.Gen.CharType.Symbol | charType);

                var minimum = gen.Minimum(seed: seed, size: size);

                minimum.Should().Be('!');
            }));

        [Property]
        public Property OtherwiseIfCharTypeHasExtended_ItShrinksToMajusculeCCedilla() => new Property(
            from charType in TestGen.CharType(
                GalaxyCheck.Gen.CharType.Whitespace,
                GalaxyCheck.Gen.CharType.Alphabetical,
                GalaxyCheck.Gen.CharType.Numeric,
                GalaxyCheck.Gen.CharType.Symbol)
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Char(GalaxyCheck.Gen.CharType.Extended | charType);

                var minimum = gen.Minimum(seed: seed, size: size);

                minimum.Should().Be('\u0080');
            }));

        [Property]
        public Property Otherwise_ItShrinksToNullCharacter() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Char(GalaxyCheck.Gen.CharType.Control);

                var minimum = gen.Minimum(seed: seed, size: size);

                minimum.Should().Be('\u0000');
            }));

        private static class TestGen
        {
            public static IGen<GalaxyCheck.Gen.CharType> CharType(params GalaxyCheck.Gen.CharType[] excludedCharType)
            {
                var allCharTypes = new[]
                {
                    GalaxyCheck.Gen.CharType.Whitespace,
                    GalaxyCheck.Gen.CharType.Alphabetical,
                    GalaxyCheck.Gen.CharType.Numeric,
                    GalaxyCheck.Gen.CharType.Symbol,
                    GalaxyCheck.Gen.CharType.Extended,
                    GalaxyCheck.Gen.CharType.Control
                };

                var candidateCharTypes = allCharTypes.Except(excludedCharType);

                return Gen
                    .Element(candidateCharTypes)
                    .ListOf()
                    .WithCountGreaterThan(0)
                    .Select(xs => xs.Aggregate((GalaxyCheck.Gen.CharType)0, (acc, curr) => acc | curr));
            }
        }
    }
}
