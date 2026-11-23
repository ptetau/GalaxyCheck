# Generators for the base types

GalaxyCheck has a generator for each C# keyword type and for the common `System` value types. The name of each
generator is the name of its `System` type, for example `Gen.UInt64()` and `Gen.Single()`. A `[Property]`
parameter and `Gen.Create<T>()` use the same generator with no option.

The [generator reference](generators.md) has one example for each generator.

## Integers

These generators make integers over the full range of the type, with the minimum and the maximum included:

| Type | Generator |
| --- | --- |
| `byte`, `sbyte` | `Gen.Byte()`, `Gen.SByte()` |
| `short`, `ushort` | `Gen.Int16()`, `Gen.UInt16()` |
| `int`, `uint` | `Gen.Int32()`, `Gen.UInt32()` |
| `long`, `ulong` | `Gen.Int64()`, `Gen.UInt64()` |
| `nint`, `nuint` | `Gen.IntPtr()`, `Gen.UIntPtr()` |
| `Int128`, `UInt128` | `Gen.Int128()`, `Gen.UInt128()` |
| `BigInteger` | `Gen.BigInteger()`, from -(2^127) to 2^127 with no bound |

Each integer generator has these options:

- `GreaterThanEqual(min)` and `LessThanEqual(max)`: the inclusive bounds.
- `Between(x, y)`: both bounds, inclusive.
- `GreaterThan(min)` and `LessThan(max)`: the exclusive bounds.
- `ShrinkTowards(origin)`: the value that a failing value shrinks to. The origin must be in the bounds.
- `WithBias(bias)`: `Gen.Bias.WithSize` (the default) grows the range with the size of the test case.
  `Gen.Bias.None` gives values uniformly over the range.

A failing value shrinks to the value in the bounds that is nearest to the origin. The default origin is zero, or
the bound that is nearest to zero.

```csharp
using GalaxyCheck;

var dice = Gen.Int32().Between(1, 6);
var ports = Gen.UInt16().GreaterThan(1023);
var big = Gen.BigInteger().GreaterThanEqual(long.MaxValue);

Property.ForAll(dice, x => x is >= 1 and <= 6).Assert();
Property.ForAll(ports, port => port > 1023).Assert();
Property.ForAll(big, x => x >= long.MaxValue).Assert();

int smallest = Gen.Int32().Between(-50, 50).ShrinkTowards(7).Minimum(seed: 0);
Console.WriteLine(smallest);
```

The program writes `7`.

## Bound errors

An option that is not valid gives a generator error. The run throws `GalaxyCheck.Exceptions.GenErrorException`.
The message names the option, and its last line starts with `Fix:`:

```csharp
using GalaxyCheck;

try
{
    Gen.Int32().GreaterThanEqual(5).LessThanEqual(4).Sample(iterations: 1);
}
catch (GalaxyCheck.Exceptions.GenErrorException error)
{
    Console.WriteLine(error.Message);
}
```

This program writes these lines:

```
Error during generation: 'min' cannot be greater than 'max'
Fix: give a minimum that is not above the maximum, for example .GreaterThanEqual(4).LessThanEqual(5), or swap the two bounds.
```

## Floating-point numbers and decimals

`Gen.Half()`, `Gen.Single()`, `Gen.Double()`, and `Gen.Decimal()` make values with a fraction part, negative
and positive values, and values over the full range of the type.

With no option, a binary floating-point generator also makes the special values: NaN, positive infinity, negative
infinity, negative zero, the smallest positive value, the minimum, and the maximum. Most seeds give each special
value in the first 100 values. The special values of `Gen.Decimal()` are zero, the minimum, and the maximum.

Each of these generators has these options:

- `GreaterThanEqual(min)`, `LessThanEqual(max)`, and `Between(x, y)`: the inclusive bounds.
- `ShrinkTowards(origin)`: the value that a failing value shrinks to.
- `WithoutSpecialValues()`: no special values. A binary floating-point generator then makes only finite values.

A generator with a bound never makes NaN or an infinity, and never makes a special value outside the bounds.

A failing value shrinks to the simplest value that still fails. Zero is simpler than a whole number, and a whole
number is simpler than a value with a fraction part. A special value shrinks to a finite value when a finite
value also fails.

```csharp
using GalaxyCheck;

var anyDouble = Gen.Double();
Console.WriteLine(anyDouble.Sample(iterations: 100, seed: 0).Any(double.IsNaN));

var finite = Gen.Double().WithoutSpecialValues();
Property.ForAll(finite, x => double.IsFinite(x)).Assert();

var probabilities = Gen.Double().Between(0.0, 1.0);
Property.ForAll(probabilities, p => p >= 0.0 && p <= 1.0).Assert();

var money = Gen.Decimal().Between(0m, 1000m);
Property.ForAll(money, m => m >= 0m && m <= 1000m).Assert();
```

The program writes `True`.

## Complex numbers

`Gen.Complex()` makes `System.Numerics.Complex` values. The real part and the imaginary part each come from
`Gen.Double()`. `WithoutSpecialValues()` removes the special values from both parts.

```csharp
using GalaxyCheck;
using System.Numerics;

var finiteComplex = Gen.Complex().WithoutSpecialValues();
Property.ForAll(finiteComplex, z => double.IsFinite(z.Real) && double.IsFinite(z.Imaginary)).Assert();
Console.WriteLine("The property holds.");
```

## Types of .NET 11

On net11.0 only, these generators also exist: `Gen.Decimal32()`, `Gen.Decimal64()`, `Gen.Decimal128()`,
`Gen.BFloat16()`, and `Gen.ComplexOf(real, imaginary)` for `Complex<T>`. The first four have the options of the
floating-point generators. On net10.0, these generators do not exist.

## Other base types

| Type | Generator | Notes |
| --- | --- | --- |
| `bool` | `Gen.Boolean()` | Shrinks to `false`. |
| `char` | `Gen.Char(charType)` | `Gen.CharType` selects the kinds of characters. |
| `Rune` | `Gen.Rune()` | Only valid Unicode scalar values, inside and outside the Basic Multilingual Plane. Shrinks to `'a'`. |
| `Guid` | `Gen.Guid()` | |
| an enum | `Gen.Enum<T>()` | Each declared value of the enum. |

For one seed and size, a generator gives the same values on net10.0 and on net11.0.

To make text, read [Text and pattern strings](text.md). To make dates and times, read
[Time types](time-types.md).
