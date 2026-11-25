# GalaxyCheck

> **Fork notice:** This repository is a fork of [nth-commit/GalaxyCheck](https://github.com/nth-commit/GalaxyCheck) by [@nth-commit](https://github.com/nth-commit). All original architecture, API design, and foundational implementation belong to upstream `nth-commit/GalaxyCheck`; this fork adds `.NET 10`/`.NET 11` targeting, a rewritten `GenSource`/`Shrinker` core, additional generators and framework integrations (`xUnit v3`, `TUnit`, `NUnit`, `MSTest`), AOT source generation, agent diagnostics, and compile-time formal verification via [Verifiable.NET](https://github.com/ptetau/Verifiable.NET).

GalaxyCheck is a property-based testing library for .NET, in the style of
[FsCheck](https://github.com/fscheck/FsCheck). The design of GalaxyCheck comes more directly from
[fast-check](https://github.com/dubzzz/fast-check), [jqwik](https://github.com/jlink/jqwik), and
[Hedgehog](https://github.com/hedgehogqa/fsharp-hedgehog).

GalaxyCheck uses LINQ expressions to join generators and properties. In GalaxyCheck, a property _is_ a generator
of tests.

GalaxyCheck builds for net10.0 and net11.0. It runs in xunit v2, xunit v3, TUnit, NUnit, and MSTest.

[![Nuget](https://img.shields.io/nuget/v/galaxycheck)](https://www.nuget.org/packages/GalaxyCheck/)

## Packages

| Package | Use |
| --- | --- |
| [GalaxyCheck](https://www.nuget.org/packages/GalaxyCheck/) | The core library: generators, properties, and runners. Use it alone, or in any test framework. |
| [GalaxyCheck.Xunit](https://www.nuget.org/packages/GalaxyCheck.Xunit/) | The `[Property]` attribute and the other attributes for xunit v2. |
| GalaxyCheck.Xunit.V3 | The same attributes for xunit v3. |
| GalaxyCheck.TUnit | The same attributes for TUnit, with the `[Test, PropertyTest]` form. |
| GalaxyCheck.NUnit | The same attributes for NUnit. |
| GalaxyCheck.MSTest | The same attributes for MSTest, with no `[GenSnapshot]`. |
| GalaxyCheck.SourceGeneration | A source generator that writes the code of `Gen.Create<T>()` at compile time, for trimming and NativeAOT. |
| [GalaxyCheck.Xunit.CodeAnalysis](https://www.nuget.org/packages/GalaxyCheck.Xunit.CodeAnalysis/) | Roslyn analyzers for GalaxyCheck.Xunit. They find a `[MemberGen]` that names a member that does not exist or has the wrong type. |

Each framework package refers to the GalaxyCheck package. Thus, you add only one package to a test project.

## Quickstart

Add the package for xunit v2:

```shell
dotnet add package GalaxyCheck.Xunit
```

Then write a property. GalaxyCheck generates the parameters of the method, and the test passes when the
property holds for each generated value.

```csharp
using GalaxyCheck;
using System.Linq;

public class AboutSort
{
    [Property]
    public Property SortIsIdempotent() =>
        Gen.Int32().ListOf().ForAll(xs => xs.Order().SequenceEqual(xs.Order().Order()));

    [Property]
    public bool AdditionIsCommutative(int x, int y) => x + y == y + x;
}
```

To use another test framework, read [Getting started](docs/getting-started.md).

## Examples

Each example states one rule of a small piece of domain code, and names the fault that the rule finds. The examples
come from the three [full demos](#the-full-demos), in a reduced form. Each block compiles and passes alone.

### Round trip: format and parse money

A round trip formats a value and parses the text again. This property finds a format with the wrong count of
decimal places. The generator `AnyMoney` is a LINQ expression: it selects a currency first, and then an amount in the minor units
of that currency.

```csharp
using GalaxyCheck;
using System;
using System.Globalization;

public record Money(decimal Amount, string Currency)
{
    public static int Places(string currency) => currency == "JPY" ? 0 : currency == "BHD" ? 3 : 2;

    public string Format() =>
        Currency + " " + Amount.ToString("N" + Places(Currency), CultureInfo.InvariantCulture);

    public static Money Parse(string text) =>
        new(decimal.Parse(text.Substring(4), NumberStyles.Number, CultureInfo.InvariantCulture), text.Substring(0, 3));
}

public class MoneyRoundTrip
{
    public static IGen<Money> AnyMoney =>
        from currency in Gen.Element(new[] { "USD", "JPY", "BHD" })
        from minorUnits in Gen.Int64().Between(-100_000_000, 100_000_000)
        select new Money(minorUnits / (decimal)Math.Pow(10, Money.Places(currency)), currency);

    [Property]
    public Property FormatThenParseGivesTheSameMoney() =>
        AnyMoney.ForAll(money => Money.Parse(money.Format()) == money);
}
```

To see a failure, we changed `Format` in a scratch copy to always write two decimal places (`"N2"`). The property
then failed with this output:

```text
GalaxyCheck.Runners.PropertyFailedException :

Falsified after 4 tests (1 shrink)
Reproduction:
    [Replay("Ao7+wAUDAwIBAA==")]
Counterexample: { Amount = 0.001, Currency = "BHD" }
Counterexample as C#:
    new global::Money(0.001m, "BHD")

Property function returned false
```

The shrunk counterexample is the smallest amount in a currency with three decimal places. This README does not show
the JSON block at the end of the output.

### Invariant: a transfer creates or loses no money

An invariant is a rule that holds after each operation. This property finds a transfer that creates money, loses
money, or takes an account below zero.

```csharp
using GalaxyCheck;
using System.Linq;

public static class Bank
{
    public static void Transfer(long[] balances, int payer, int payee, long cents)
    {
        if (cents <= 0 || balances[payer] < cents) return;
        balances[payer] -= cents;
        balances[payee] += cents;
    }
}

public class TransferInvariant
{
    [Property]
    public Property TransfersKeepTheTotal() =>
        Property.ForAll(
            Gen.Int64().Between(0, 1_000).ListOf().WithCount(3),
            Gen.Zip(Gen.Int32().Between(0, 2), Gen.Int32().Between(0, 2), Gen.Int64().Between(-10, 1_000)).ListOf(),
            (start, transfers) =>
            {
                var balances = start.ToArray();
                foreach (var (payer, payee, cents) in transfers) Bank.Transfer(balances, payer, payee, cents);
                return balances.Sum() == start.Sum() && balances.All(b => b >= 0);
            });
}
```

### Oracle: compare a fast version with a slow version

An oracle is a simple, slow version of the code that is obviously right. This property finds a fast split that
gives the leftover cents to the wrong parts.

```csharp
using GalaxyCheck;
using System.Linq;

public static class Split
{
    public static long[] Fast(long cents, int parts) =>
        Enumerable.Range(0, parts).Select(i => cents / parts + (i < cents % parts ? 1 : 0)).ToArray();

    public static long[] OneCentAtATime(long cents, int parts)
    {
        var result = new long[parts];
        for (long i = 0; i < cents; i++) result[i % parts]++;
        return result;
    }
}

public class SplitOracle
{
    [Property]
    public Property FastSplitAgreesWithTheSlowSplit() =>
        Property.ForAll(Gen.Int64().Between(0, 10_000), Gen.Int32().Between(1, 12),
            (cents, parts) => Split.Fast(cents, parts).SequenceEqual(Split.OneCentAtATime(cents, parts)));
}
```

### Metamorphic: a change to the input that must not change the output

A metamorphic property changes the input in a known way, and compares the two outputs. You do not need the correct
output. This property finds an email key that changes with the case, white space at the ends, or a sub-address.

```csharp
using GalaxyCheck;

public static class Email
{
    public static string Key(string address)
    {
        var parts = address.Trim().ToLowerInvariant().Split('@');
        return parts[0].Split('+')[0] + "@" + parts[1];
    }
}

public class EmailMetamorphic
{
    [Property]
    public Property CaseSpacesAndSubAddressDoNotMatter() =>
        Property.ForAll(
            Gen.StringMatching("[a-z][a-z0-9.]{0,8}@[a-z]{1,8}\\.com"),
            Gen.StringMatching("[a-z0-9]{0,5}"),
            (address, tag) =>
            {
                var at = address.IndexOf('@');
                var messy = " " + address.Substring(0, at).ToUpperInvariant() + "+" + tag + address.Substring(at) + " ";
                return Email.Key(messy) == Email.Key(address);
            });
}
```

### Commutative and idempotent: merge two contacts

A merge must not depend on the order of its arguments, and a second merge must change nothing. This property
finds a merge that breaks a tie of dates by the argument order, or that adds an address two times.

```csharp
using GalaxyCheck;
using System.Collections.Generic;
using System.Linq;

public record Contact(string Name, int LastActivityDay, IReadOnlySet<string> Emails)
{
    public static Contact Merge(Contact a, Contact b)
    {
        var recent = a.LastActivityDay > b.LastActivityDay
            || (a.LastActivityDay == b.LastActivityDay && string.CompareOrdinal(a.Name, b.Name) <= 0) ? a : b;
        return new(recent.Name, recent.LastActivityDay, a.Emails.Union(b.Emails).ToHashSet());
    }

    public bool Same(Contact c) => Name == c.Name && LastActivityDay == c.LastActivityDay && Emails.SetEquals(c.Emails);
}

public class MergeLaws
{
    public static IGen<Contact> AnyContact =>
        from name in Gen.Element(new[] { "Ann", "Bob" })
        from day in Gen.Int32().Between(1, 3)
        from emails in Gen.Element(new[] { "a@x.com", "b@x.com" }).ListOf().WithCountLessThanEqual(2)
        select new Contact(name, day, emails.ToHashSet());

    [Property]
    public Property MergeIsCommutativeAndIdempotent() =>
        Property.ForAll(AnyContact, AnyContact, (a, b) =>
            Contact.Merge(a, b).Same(Contact.Merge(b, a)) && Contact.Merge(Contact.Merge(a, b), b).Same(Contact.Merge(a, b)));
}
```

### Tolerance: momentum in a collision

Floating-point code seldom gives the exact answer, so a numeric property compares with a tolerance. This property
finds an impulse that uses the wrong mass.

```csharp
using GalaxyCheck;
using System;

public static class Collision
{
    public static (double, double) Elastic(double m1, double v1, double m2, double v2) =>
        (((m1 - m2) * v1 + 2 * m2 * v2) / (m1 + m2), ((m2 - m1) * v2 + 2 * m1 * v1) / (m1 + m2));
}

public class CollisionTolerance
{
    [Property]
    public Property MomentumIsConserved() =>
        Property.ForAll(Gen.Double().Between(0.001, 1_000), Gen.Double().Between(-100, 100).Two(),
            Gen.Double().Between(0.001, 1_000), (m1, velocities, m2) =>
            {
                var (v1, v2) = velocities;
                var (u1, u2) = Collision.Elastic(m1, v1, m2, v2);
                var scale = m1 * Math.Abs(v1) + m2 * Math.Abs(v2);
                return Math.Abs(m1 * u1 + m2 * u2 - (m1 * v1 + m2 * v2)) <= 1e-9 * scale + 1e-300;
            });
}
```

### Explicit examples beside generated inputs

`[Example]` runs a known edge case before the generated inputs. The second example is a rounding error below
absolute zero, and finds a check of absolute zero with no tolerance. The second property is a round trip with a
tolerance.

```csharp
using GalaxyCheck;
using System;

public static class Temperature
{
    public static double FahrenheitToKelvin(double f)
    {
        var kelvin = (f - 32) * 5 / 9 + 273.15;
        if (kelvin < -1e-9) throw new ArgumentOutOfRangeException(nameof(f), "Below absolute zero");
        return Math.Max(kelvin, 0);
    }

    public static double KelvinToFahrenheit(double k) => (k - 273.15) * 9 / 5 + 32;
}

public class TemperatureExamples
{
    public static IGen<double> Fahrenheit => Gen.Double().Between(-459.67, 10_000);

    [Property]
    [Example(-459.67)]
    [Example(-459.6700000000001)]
    public bool AbsoluteZeroIsNotAnError([MemberGen(nameof(Fahrenheit))] double f) =>
        Temperature.FahrenheitToKelvin(f) >= 0;

    [Property]
    public Property ConversionRoundTrips() =>
        Fahrenheit.ForAll(f => Math.Abs(Temperature.KelvinToFahrenheit(Temperature.FahrenheitToKelvin(f)) - f) <= 1e-9 * Math.Max(1, Math.Abs(f)));
}
```

### Precondition: discard the cases that do not apply

`Property.Precondition` discards a test case that does not meet a condition. Use a precondition only when most
cases meet it. This property finds an overdraft check that refuses a legal withdrawal.

```csharp
using GalaxyCheck;
using System;

public record Account(long Cents, long Limit)
{
    public Account Withdraw(long cents) =>
        cents <= Cents + Limit ? this with { Cents = Cents - cents } : throw new InvalidOperationException("No funds");
}

public class WithdrawalPrecondition
{
    [Property]
    public Property WithdrawalWithinTheLimitTakesTheExactAmount() =>
        Property.ForAll(Gen.Int64().Between(0, 1_000), Gen.Int64().Between(0, 200), Gen.Int64().Between(1, 1_000),
            (cents, limit, amount) =>
            {
                Property.Precondition(amount <= cents + limit);
                return new Account(cents, limit).Withdraw(amount).Cents == cents - amount;
            });
}
```

### Model-based: compare a system with a simple model

A model-based test runs random commands on the real system and on a simple model, and compares the two after each
command. This test finds a wallet that refuses a legal withdrawal, or that adds the wrong amount. A failure gives the
shortest command sequence that it found. Read [Stateful testing](docs/stateful-testing.md).

```csharp
using GalaxyCheck;
using GalaxyCheck.Stateful;
using System;

public class Wallet
{
    public long Cents { get; private set; }
    public void Add(long cents) => Cents = Cents + cents >= 0 ? Cents + cents : throw new InvalidOperationException();
}

public class Change : Command<Wallet, long>
{
    private readonly long cents;
    public Change(long cents) => this.cents = cents;
    public override bool Precondition(long model) => model + cents >= 0;
    public override void Run(Wallet wallet) => wallet.Add(cents);
    public override long NextModel(long model) => model + cents;
    public override bool Postcondition(Wallet wallet, long before, long after) => wallet.Cents == after;
    public override string ToString() => cents >= 0 ? $"Deposit {cents}" : $"Withdraw {-cents}";
}

public class WalletModel
{
    [Property]
    public Property WalletAgreesWithTheModel() =>
        StatefulProperty.Create(() => new Wallet(), () => 0L,
            model => Gen.Int64().Between(-model, 100).Select(cents => (Command<Wallet, long>)new Change(cents)));
}
```

### The full demos

The folder [examples](examples/README.md) holds three demos with real domain code, more properties, and the
bugs that each property found in a scratch copy.

| Demo | Domain | Test framework | Run the tests |
| --- | --- | --- | --- |
| [Banking](examples/Banking/README.md) | Money, a ledger, transfers, interest | xunit v3 | `cd examples/Banking/Banking.Tests && dotnet test` |
| [Physics](examples/Physics/README.md) | Vectors, units, collisions, a spring, an orbit | TUnit | `cd examples/Physics && dotnet test --project Physics.Tests` |
| [CRM](examples/Crm/README.md) | Contacts, normalisation, merge, a pipeline, search | NUnit | `cd examples/Crm/Crm.Tests && dotnet test` |

## A tour of the features

### Shrinking

When a property fails, GalaxyCheck shrinks the counterexample to a small value that still fails. Each shrunk
value obeys the constraints of its generator.

This property is wrong on purpose:

```csharp
using GalaxyCheck;

public class AboutShrinking
{
    [Property]
    public Property MyProperty() =>
        Property.ForAll(
            from x in Gen.Int32().GreaterThanEqual(1)
            from y in Gen.Int32().GreaterThanEqual(x + 5)
            select (x, y),
            input => input.x < 10 || input.y < input.x);
}
```

The value `x` is sometimes less than 10, but `y` is always at least `x + 5`. Thus, the property fails. GalaxyCheck
gives the smallest counterexample, a replay string, and a C# expression of the counterexample:

```
GalaxyCheck.Runners.PropertyFailedException :

Falsified after 11 tests (1 shrink)
Reproduction:
    [Replay("ArHH1ZcECgIJAA==")]
Counterexample:
    [0] = 10
    [1] = 15
Counterexample as C#:
    (10, 15)

Property function returned false

<galaxycheck-failure>
{"counterexample":["(Item1 = 10, Item2 = 15)"],"csharp":["(10, 15)"],"replay":"ArHH1ZcECgIJAA==","seed":1123378097,"size":10,"iterations":11,"shrinks":1,"kind":"generated","test":"AboutShrinking.MyProperty","rerun":"dotnet test --filter FullyQualifiedName=AboutShrinking.MyProperty"}
</galaxycheck-failure>
```

The seed is random for each run. Thus, the count of tests and the replay string change from run to run. The
block of JSON at the end is for a tool or an agent. Read [Use by LLM agents](docs/agents.md).

GalaxyCheck uses integrated shrinking. Each generator knows how to shrink its values, so you do not write a
shrink function. When you join generators with LINQ, the shrinks of each generator join too.

### Composability

A custom generator is a LINQ expression. This generator makes a point:

```csharp
using GalaxyCheck;

public static class Points
{
    public static IGen<(int x, int y)> Point =>
        from x in Gen.Int32()
        from y in Gen.Int32()
        select (x, y);
}
```

To make only points with no negative coordinate, add a bound to each generator:

```csharp
using GalaxyCheck;

public static class PositivePoints
{
    public static IGen<(int x, int y)> Point =>
        from x in Gen.Int32().GreaterThanEqual(0)
        from y in Gen.Int32().GreaterThanEqual(0)
        select (x, y);
}
```

A range has a minimum and a maximum in order. The second generator reads the value of the first generator:

```csharp
using GalaxyCheck;

public static class Ranges
{
    public static IGen<(int min, int max)> Range =>
        from min in Gen.Int32().GreaterThanEqual(0)
        from max in Gen.Int32().GreaterThanEqual(min)
        select (min, max);
}
```

### Repeatability

The failure message of the shrinking example holds the attribute `[Replay("ArHH1ZcECgIJAA==")]`. Put the attribute on
the property, and the test runs only the counterexample:

```csharp
using GalaxyCheck;

public class AboutReplaying
{
    [Property]
    [Replay("ArHH1ZcECgIJAA==")]
    public Property MyProperty() =>
        Property.ForAll(
            from x in Gen.Int32().GreaterThanEqual(1)
            from y in Gen.Int32().GreaterThanEqual(x + 5)
            select (x, y),
            input => input.x < 10 || input.y < input.x);
}
```

Many frameworks give only the seed of a failure. Then the debugger stops at each test case before the
counterexample, and at each shrink. A replay string of GalaxyCheck holds the exact choices of the counterexample.
Thus, the debugger goes directly to the counterexample.

GalaxyCheck also saves each failure in the file `.galaxycheck/failures.json`, and the next run tries the failure
first. Read [Failure memory](docs/failure-memory.md).

### Transparency

The `[Sample]` attribute replaces `[Property]`. The test then fails with a list of the values that the property
receives:

```csharp
using GalaxyCheck;

public class AboutSampling
{
    [Sample]
    public Property MyProperty() =>
        Property.ForAll(
            from x in Gen.Int32().GreaterThanEqual(1)
            from y in Gen.Int32().GreaterThanEqual(x + 5)
            select (x, y),
            input => input.x < 10 || input.y < input.x);
}
```

The sample shows a problem in the test. When `x` is near `int.MaxValue`, `x + 5` overflows. Thus, the generator
of `x` needs an upper bound:

```csharp
using GalaxyCheck;

public class AboutSamplingWithABound
{
    [Sample]
    public Property MyProperty() =>
        Property.ForAll(
            from x in Gen.Int32().Between(1, int.MaxValue - 5)
            from y in Gen.Int32().GreaterThanEqual(x + 5)
            select (x, y),
            input => input.x < 10 || input.x > input.y);
}
```

### Generators for the base types

GalaxyCheck has a generator for each C# base type, for the time types, for containers, and for text. A
floating-point generator also gives the special values, for example NaN and the infinities:

```csharp
using GalaxyCheck;
using System;

public class AboutGenerators
{
    [Property]
    public Property PricesRoundTripThroughText() =>
        Gen.Decimal().Between(0m, 1000m).ForAll(price =>
            decimal.Parse(price.ToString(System.Globalization.CultureInfo.InvariantCulture),
                System.Globalization.CultureInfo.InvariantCulture) == price);

    [Property]
    public Property DatesAreInRange() =>
        Gen.DateOnly().From(new DateOnly(2020, 1, 1)).ForAll(date => date.Year >= 2020);

    [Property]
    public Property CodesHaveTheFormat() =>
        Gen.StringMatching("[A-Z]{2}-[0-9]{3}").ForAll(code => code.Length == 6);
}
```

`Gen.Create<T>()` builds a generator for a record or a class from the generators of its members.

### Property features

- [Explicit examples](docs/explicit-examples.md): `[Example(...)]` gives fixed inputs that run first.
- [Exhaustive runs](docs/exhaustive-runs.md): a small input space runs each value one time.
- [Preconditions and filters](docs/preconditions-and-filters.md): `Property.Precondition` discards a test case.
- [Labels and cover](docs/labels-and-cover.md): the report gives the share of each label.
- [Health checks](docs/health-checks.md): a property that discards too much fails with a clear message.
- [Timeouts and cancellation](docs/timeouts-and-cancellation.md): a slow test case fails the property.
- [Stateful testing](docs/stateful-testing.md): sequences of commands against a model.

### Performance

The current engine is fast. In one measured run on one machine, each benchmark case ran between 142 and 1,460
times the throughput of the old engine. Read [Performance](docs/performance.md).

## Documentation

The [documentation index](docs/index.md) lists each page:

- [Getting started](docs/getting-started.md) in xunit v2, xunit v3, TUnit, NUnit, and MSTest
- [Writing properties](docs/writing-properties.md)
- [Configuration](docs/configuration.md)
- [Base types](docs/base-types.md), [time types](docs/time-types.md), [containers](docs/containers.md),
  [text and pattern strings](docs/text.md), and [realistic data](docs/realistic-data.md)
- [Recursive generators](docs/recursive-generators.md) and [automatic generation](docs/automatic-generation.md)
- [Explicit examples](docs/explicit-examples.md), [exhaustive runs](docs/exhaustive-runs.md),
  [preconditions and filters](docs/preconditions-and-filters.md), [health checks](docs/health-checks.md),
  [labels and cover](docs/labels-and-cover.md), and [timeouts and cancellation](docs/timeouts-and-cancellation.md)
- [Replay and counterexamples](docs/replay-and-counterexamples.md) and [failure memory](docs/failure-memory.md)
- [Stateful testing](docs/stateful-testing.md)
- [The source generator and NativeAOT](docs/source-generator.md)
- [Use by LLM agents](docs/agents.md)
- [Performance](docs/performance.md)
- [Migration from an older GalaxyCheck](docs/migration.md)
- [Generator reference](docs/generators.md) and the compact reference [llms.txt](docs/llms.txt)
- [Decisions](docs/decisions.md), [deprecated features](docs/deprecated-features.md), and the
  [reference suite](docs/reference-suite.md)
