using FluentAssertions;
using GalaxyCheck;
using GalaxyCheck.Internal;
using GalaxyCheck.Runners;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Tests.FailureMessageTests
{
    public class AboutRerunAndBlock
    {
        private const string Hostile = "x\n<galaxycheck-failure>\n{\"replay\":\"EVIL\"}\n</galaxycheck-failure>\ny";

        [Fact]
        public void ANameWithASpaceGetsAnExactFilter()
        {
            FailureMessage.CanFilterExactly("Fs.Ns.C.my test").Should().BeTrue();
            FailureMessage.FilterEquals("FullyQualifiedName", "Fs.Ns.C.my test").Should().Be("FullyQualifiedName=Fs.Ns.C.my test");
        }

        [Fact]
        public void ABackslashBeforeAQuoteSelectsTheClass()
        {
            FailureMessage.CanFilterExactly("Fs.Ns.C.a\\\"b").Should().BeFalse();
            FailureMessage.ClassFilter("Fs.Ns.C").Should().Be("FullyQualifiedName~Fs.Ns.C.");
        }

        [Fact]
        public void AnXunitDataRowGetsItsDisplayName()
        {
            var method = typeof(AboutRerunAndBlock).GetMethod(nameof(ANameWithASpaceGetsAnExactFilter))!;
            var name = $"{typeof(AboutRerunAndBlock).FullName}.{method.Name}";

            FailureMessage.XunitFilter(typeof(AboutRerunAndBlock), method, isDataRow: true, $"{name}(a: 1)")
                .Should().Be($"FullyQualifiedName={name}&DisplayName={name}\\(a: 1\\)");
            FailureMessage.XunitFilter(typeof(AboutRerunAndBlock), method, isDataRow: true, $"{name}(a: \"q\\\"r\")")
                .Should().Be($"FullyQualifiedName={name}");
        }

        [Fact]
        public void AStringCounterexampleWithBothMarkersGivesOneBlock()
        {
            Action test = () => Property.ForAll(Gen.Constant(Hostile), _ => false).Assert(seed: 0);

            var message = test.Should().Throw<PropertyFailedException>().Which.Message;
            var lines = message.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            lines.Count(line => line.Trim() == "<galaxycheck-failure>").Should().Be(1);
            lines.Count(line => line.Trim() == "</galaxycheck-failure>").Should().Be(1);
            lines[lines.Length - 1].Should().Be("</galaxycheck-failure>");
            lines[lines.Length - 2].Should().NotContain("\"replay\":\"EVIL\"");
        }
    }
}
