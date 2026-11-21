using FluentAssertions;
using GalaxyCheck;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Tests.V2.GenTests.CharGenTests
{
    public class AboutValueProduction
    {
        [Property]
        public Property IfCharTypeIsWhitespace_ItOnlyProducesWhitespaceCharacters() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Char(GalaxyCheck.Gen.CharType.Whitespace);

                var sample = SampleTraversal(gen, seed, size);

                sample.Should().OnlyContain(c => new Regex(@"\s").IsMatch(c.ToString()));
            }));

        [Property]
        public Property IfCharTypeIsAlphabetic_ItOnlyProducesAlphabeticCharacters() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Char(GalaxyCheck.Gen.CharType.Alphabetical);

                var sample = SampleTraversal(gen, seed, size);

                sample.Should().OnlyContain(c => new Regex(@"[A-Z]|[a-z]").IsMatch(c.ToString()));
            }));

        [Property]
        public Property IfCharTypeIsNumeric_ItOnlyProducesNumericCharacters() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Char(GalaxyCheck.Gen.CharType.Numeric);

                var sample = SampleTraversal(gen, seed, size);

                sample.Should().OnlyContain(c => new Regex(@"[0-9]").IsMatch(c.ToString()));
            }));

        [Property]
        public Property IfCharTypeIsSymbol_ItOnlyProducesSymbolCharacters() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Char(GalaxyCheck.Gen.CharType.Symbol);

                var sample = SampleTraversal(gen, seed, size);

                sample.Should().OnlyContain(c =>
                    new Regex(@"\D").IsMatch(c.ToString()) &&
                    new Regex(@"\S").IsMatch(c.ToString()));
            }));

        [Property]
        public Property IfCharTypeIsExtended_ItOnlyProducesCharactersInExtendedAsciiSet() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Char(GalaxyCheck.Gen.CharType.Extended);

                var sample = SampleTraversal(gen, seed, size);

                sample.Should().OnlyContain(c => c >= 128);
            }));

        [Property]
        public Property IfCharTypeIsControl_ItOnlyProducesCharactersInControlAsciiSet() => new Property(
            from seed in DomainGen.Seed()
            from size in DomainGen.Size()
            select TestProperty.ForThese(() =>
            {
                var gen = GalaxyCheck.Gen.Char(GalaxyCheck.Gen.CharType.Control);

                var sample = SampleTraversal(gen, seed, size);

                sample.Should().OnlyContain(c => c <= 37 || c == 127);
            }));

        private static List<char> SampleTraversal(GalaxyCheck.IGen<char> gen, int seed, int size) => gen.SampleOneTraversal(seed: seed, size: size)
            .Take(100)
            .ToList();
    }
}
