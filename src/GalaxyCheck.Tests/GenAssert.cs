using FluentAssertions;
using GalaxyCheck;
using Newtonsoft.Json;
using System;
using System.Linq;
using Xunit;

namespace Tests.V2
{
    public static class GenAssert
    {
        public static void Equal<T>(IGen<T> expected, IGen<T> actual, int seed, int? iterations = null)
        {
            var expectedSample = expected.Select(x => JsonConvert.SerializeObject(x)).SampleNTraversals(iterations ?? 100, seed, 50);
            var actualSample = actual.Select(x => JsonConvert.SerializeObject(x)).SampleNTraversals(iterations ?? 100, seed, 50);

            Assert.All(
                Enumerable.Zip(expectedSample, actualSample),
                (x) =>
                {
                    Assert.Equal(x.First.Take(10), x.Second.Take(10));
                });
        }

        public static void Errors<T>(IGen<T> gen, int? seed = null, string? expectedMessage = null)
        {
            Action test = () => gen.SampleOne(seed: seed ?? 0);

            if (expectedMessage == null)
            {
                test.Should()
                    .Throw<Exceptions.GenErrorException>();
            }
            else
            {
                test.Should()
                    .Throw<Exceptions.GenErrorException>()
                    .WithGenErrorMessage(expectedMessage);
            }
        }
    }
}
