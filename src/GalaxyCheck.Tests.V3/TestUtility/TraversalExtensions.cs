using GalaxyCheck.Gens.Internal;
using GalaxyCheck.Gens.Parameters;
using GalaxyCheck.Runners;

namespace GalaxyCheck.Tests.TestUtility;

public static class TraversalExtensions
{
    /// <summary>
    /// Samples one value, then each shrink that the shrinker makes when every value is a counterexample.
    /// </summary>
    public static List<T> SampleOneTraversal<T>(this IGen<T> gen, int seed, int size, int maxCount)
    {
        var source = GenSource.Create(seed, size);
        source.BeginRandom(size);

        T root;
        try
        {
            root = gen.Advanced.Generate(source);
        }
        catch (GenErrorSignal signal)
        {
            throw new Exceptions.GenErrorException("Error during generation: " + signal.Message, null);
        }
        finally
        {
            source.End();
        }

        var values = new List<T> { root };
        var shrinker = new Shrinker(source, int.MaxValue);

        foreach (var _ in shrinker.Candidates())
        {
            if (values.Count >= maxCount) break;

            source.BeginReplay(shrinker.Candidate, shrinker.CandidateLength, size);
            T value;
            try
            {
                value = gen.Advanced.Generate(source);
            }
            catch (Exception)
            {
                continue;
            }
            finally
            {
                source.End();
            }

            if (shrinker.TryAccept(source))
            {
                values.Add(value);
            }
        }

        return values;
    }
}
