using System.Collections;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace GalaxyCheck.Tests.Slice;

/// <summary>Helpers of the S6 tests.</summary>
internal static class S6Support
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>Gives an exact text for a value, so that two lists compare bit for bit.</summary>
    public static string Show(object? value) => value switch
    {
        null => "null",
        string s => s,
        double d => d.ToString("R", Inv),
        float f => f.ToString("R", Inv),
        Half h => h.ToString("R", Inv),
        Complex c => $"({Show(c.Real)},{Show(c.Imaginary)})",
        Rune r => "U+" + r.Value.ToString("X", Inv),
        DateTimeOffset o => o.ToString("O", Inv),
        DateOnly d => d.ToString("O", Inv),
        TimeOnly t => t.ToString("O", Inv),
        TimeSpan t => t.ToString("c", Inv),
        IFormattable f => f.ToString(null, Inv),
        IEnumerable e => "[" + string.Join(", ", e.Cast<object?>().Select(Show)) + "]",
        _ => value.ToString() ?? "",
    };

    public static List<string> ShowAll<T>(IEnumerable<T> values) => values.Select(v => Show(v)).ToList();

    /// <summary>Samples 100 values of the automatic generator and of the explicit generator with seed 0, and compares them.</summary>
    public static void AssertCreateEqualsExplicit<T>(IGen<T> explicitGen, List<string> problems)
    {
        try
        {
            var automatic = ShowAll(Gen.Create<T>().Sample(iterations: 100, seed: 0));
            var explicitValues = ShowAll(explicitGen.Sample(iterations: 100, seed: 0));
            if (automatic.Count != 100 || explicitValues.Count != 100)
            {
                problems.Add($"{typeof(T).Name}: expected 100 values, got {automatic.Count} and {explicitValues.Count}");
            }
            else if (!automatic.SequenceEqual(explicitValues))
            {
                problems.Add($"{typeof(T).Name}: Gen.Create and the explicit generator gave different lists");
            }
        }
        catch (Exception e)
        {
            problems.Add($"{typeof(T).Name}: {e.GetType().Name}: {e.Message}");
        }
    }
}
