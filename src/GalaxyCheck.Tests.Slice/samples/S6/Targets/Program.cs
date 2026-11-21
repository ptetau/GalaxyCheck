using System.Collections;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Text;
using GalaxyCheck;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

// Each line is "<group> <name> <values>". The test compares the lines of net10.0 and net11.0.
void Emit<T>(string group, string name, IGen<T> gen)
{
    try
    {
        var values = gen.Sample(iterations: 100, seed: 0, size: 50);
        Console.WriteLine($"{group} {name} {values.Count} " + string.Join("|", values.Select(v => Show(v))));
    }
    catch (Exception e)
    {
        Console.WriteLine($"{group}-ERROR {name} {e.GetType().Name}: {e.Message.Replace('\n', ' ')}");
    }
}

static string Show(object? value) => value switch
{
    null => "null",
    string s => s,
    double d => d.ToString("R", CultureInfo.InvariantCulture),
    float f => f.ToString("R", CultureInfo.InvariantCulture),
    Half h => h.ToString("R", CultureInfo.InvariantCulture),
    Complex c => $"({Show(c.Real)},{Show(c.Imaginary)})",
    Rune r => "U+" + r.Value.ToString("X", CultureInfo.InvariantCulture),
    DateTimeOffset o => o.ToString("O", CultureInfo.InvariantCulture),
    DateOnly d => d.ToString("O", CultureInfo.InvariantCulture),
    TimeOnly t => t.ToString("O", CultureInfo.InvariantCulture),
    TimeSpan t => t.ToString("c", CultureInfo.InvariantCulture),
    IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
    IEnumerable e => "[" + string.Join(", ", e.Cast<object?>().Select(Show)) + "]",
    _ => value.ToString() ?? "",
};

Console.WriteLine($"RUNTIME {Environment.Version.Major}");
var wanted = new[] { "Decimal32", "Decimal64", "Decimal128", "BFloat16", "ComplexOf" };
var present = typeof(Gen).GetMethods(BindingFlags.Public | BindingFlags.Static).Select(m => m.Name).ToHashSet();
Console.WriteLine("MEMBERS " + string.Join(",", wanted.Where(present.Contains)));

Emit("GEN", "SByte", Gen.SByte());
Emit("GEN", "UInt16", Gen.UInt16());
Emit("GEN", "UInt32", Gen.UInt32());
Emit("GEN", "UInt64", Gen.UInt64());
Emit("GEN", "IntPtr", Gen.IntPtr());
Emit("GEN", "UIntPtr", Gen.UIntPtr());
Emit("GEN", "Int128", Gen.Int128());
Emit("GEN", "UInt128", Gen.UInt128());
Emit("GEN", "BigInteger", Gen.BigInteger());
Emit("GEN", "Half", Gen.Half());
Emit("GEN", "Single", Gen.Single());
Emit("GEN", "Double", Gen.Double());
Emit("GEN", "Decimal", Gen.Decimal());
Emit("GEN", "Complex", Gen.Complex());
Emit("GEN", "TimeSpan", Gen.TimeSpan());
Emit("GEN", "DateTimeOffset", Gen.DateTimeOffset());
Emit("GEN", "DateOnly", Gen.DateOnly());
Emit("GEN", "TimeOnly", Gen.TimeOnly());
Emit("GEN", "Rune", Gen.Rune());
Emit("GEN", "Dictionary", Gen.Dictionary(Gen.Int32(), Gen.Boolean()));
Emit("GEN", "KeyValuePair", Gen.KeyValuePair(Gen.Int32(), Gen.Boolean()));
Emit("GEN", "ValueTuple", Gen.ValueTuple(Gen.Int32(), Gen.Boolean()));
Emit("GEN", "Tuple", Gen.Tuple(Gen.Int32(), Gen.Boolean()));

Emit("TEXT", "StringMatching", Gen.StringMatching(@"[a-c]{2,4}-\d{3}"));
Emit("TEXT", "Uri", Gen.Uri());
Emit("TEXT", "Version", Gen.Version());
Emit("TEXT", "Email", Gen.Email());
Emit("TEXT", "FirstName", Gen.Realistic.FirstName());
Emit("TEXT", "LastName", Gen.Realistic.LastName());
Emit("TEXT", "FullName", Gen.Realistic.FullName());
Emit("TEXT", "StreetAddress", Gen.Realistic.StreetAddress());
Emit("TEXT", "City", Gen.Realistic.City());
Emit("TEXT", "Country", Gen.Realistic.Country());
Emit("TEXT", "PhoneNumber", Gen.Realistic.PhoneNumber());
Emit("TEXT", "CompanyName", Gen.Realistic.CompanyName());
Emit("TEXT", "Recursive", Gen.Recursive<int>(Gen.Int32(), g => g.Select(x => x + 1)));

#if NET11_0_OR_GREATER
Emit("N11", "Decimal32", Gen.Decimal32());
Emit("N11", "Decimal64", Gen.Decimal64());
Emit("N11", "Decimal128", Gen.Decimal128());
Emit("N11", "BFloat16", Gen.BFloat16());
#endif
