using System.Globalization;
using GalaxyCheck;
using GalaxyCheck.Gens;

// Runs one property on the seeds 0 to 19 and prints the result of each run between two marker lines.
// Two processes that run this program must print the same text.
var property = Property.ForAll(Gen.Int32().ListOf(), xs => xs.Count(x => x > 10) < 2);

Console.WriteLine("<s13-runs>");
for (var seed = 0; seed < 20; seed++)
{
    var result = property.Check(iterations: 100, seed: seed);
    var value = result.Counterexample is null
        ? "none"
        : "[" + string.Join(", ", result.Counterexample.Value.Select(x => x.ToString(CultureInfo.InvariantCulture))) + "]";
    Console.WriteLine($"seed {seed}: falsified {result.Falsified}, iterations {result.Iterations}, shrinks {result.Shrinks}, counterexample {value}, replay {result.Counterexample?.Replay ?? "none"}");
}

Console.WriteLine("</s13-runs>");
