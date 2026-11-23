# Generators for text

GalaxyCheck makes free text, strings that match a regular expression, and common text values: URIs, versions,
and email addresses. For names and addresses, read [Realistic data](realistic-data.md).

## Strings and characters

`Gen.String()` makes strings, and `Gen.Char(charType)` makes characters. `Gen.CharType` is a set of flags:
`Whitespace`, `Alphabetical`, `Numeric`, `Symbol`, `Extended`, `Control`, and `All`.

The string generator has these options:

- `FromCharacters(charType)` or `FromCharacters(chars)`: the characters to use.
- `WithLength(length)`: an exact length.
- `WithLengthGreaterThanEqual(min)`, `WithLengthLessThanEqual(max)`, and `WithLengthBetween(x, y)`: inclusive
  bounds of the length.
- `WithLengthGreaterThan(min)` and `WithLengthLessThan(max)`: exclusive bounds of the length.
- `WithLengthBias(bias)`: the bias of the length.

```csharp
using GalaxyCheck;

var words = Gen.String().FromCharacters(Gen.CharType.Alphabetical).WithLengthBetween(1, 10);
var binary = Gen.String().FromCharacters("01").WithLength(8);

Property.ForAll(words, w => w.All(char.IsLetter)).Assert();
Property.ForAll(binary, b => b.Length == 8 && b.All(c => c == '0' || c == '1')).Assert();
Console.WriteLine("Both properties hold.");
```

## Pattern strings

`Gen.StringMatching(pattern)` makes strings that match a regular expression. Each value matches the pattern, and
a failing value shrinks to a shorter or simpler string that also matches.

The generator supports these constructs:

- Literals, and escapes such as `\d`, `\w`, and `\s`.
- Character classes, for example `[a-z0-9_]`.
- Alternation and groups, for example `(com|org)`.
- The quantifiers `?`, `*`, `+`, `{n}`, `{n,}`, and `{n,m}`.
- The anchors `^` and `$` at the ends of the pattern.

An unbounded quantifier (`*`, `+`, or `{n,}`) adds at most 20 repeats. The generator builds each string from
the pattern. It does not run the pattern as a match against input.

```csharp
using GalaxyCheck;
using System.Text.RegularExpressions;

var pattern = "[A-Z]{2}-[0-9]{3}";
var codes = Gen.StringMatching(pattern);

Property.ForAll(codes, code => Regex.IsMatch(code, "^" + pattern + "$")).Assert();
Console.WriteLine(string.Join(", ", codes.Sample(iterations: 5, seed: 0)));
```

The program writes `AA-000, DB-000, EB-001, PG-497, GF-393`.

A construct that the generator does not support, for example a lookahead, a lookbehind, or a backreference,
gives a generator error that names the construct:

```csharp
using GalaxyCheck;

try
{
    Gen.StringMatching("(?<=a)b").Sample(iterations: 1);
}
catch (GalaxyCheck.Exceptions.GenErrorException error)
{
    Console.WriteLine(error.Message);
}
```

This program writes these lines:

```
Error during generation: Gen.StringMatching does not support the lookbehind at position 0 of the pattern
Fix: remove the lookbehind at position 0 from the pattern, or generate with Gen.String() and filter with .Where(...).
```

## URIs, versions, and email addresses

| Generator | Values | A failing value shrinks to |
| --- | --- | --- |
| `Gen.Uri()` | Absolute `http` and `https` URIs, with a host and up to three path segments. | `http://a.com/` |
| `Gen.Version()` | `System.Version` values with 2 to 4 parts. Each part is from 0 to 1000. | `0.0` |
| `Gen.Email()` | Strings with one `@`, a local part, and a domain with a dot. | `a@a.com` |

`Gen.Create<Uri>()` uses `Gen.Uri()`, and `Gen.Create<Version>()` uses `Gen.Version()`.

```csharp
using GalaxyCheck;

Property.ForAll(Gen.Uri(), uri => uri.IsAbsoluteUri && uri.Scheme.StartsWith("http")).Assert();
Property.ForAll(Gen.Version(), version => version.Major >= 0 && version.Minor >= 0).Assert();
Property.ForAll(Gen.Email(), email => email.Count(c => c == '@') == 1).Assert();

Console.WriteLine(Gen.Uri().Minimum(seed: 0));
Console.WriteLine(Gen.Version().Minimum(seed: 0));
Console.WriteLine(Gen.Email().Minimum(seed: 0));
```

The program writes `http://a.com/`, `0.0`, and `a@a.com` on three lines.
