# Getting started

GalaxyCheck runs properties in five test frameworks. Each framework has its own package, and each package has the
same attributes: `[Property]`, `[Sample]`, `[Replay]`, `[MemberGen]`, `[Example]`, and `[SuppressHealthCheck]`.
Each package except the MSTest package also has `[GenSnapshot]`.

| Test framework | Package | Property attribute | Data rows |
| --- | --- | --- | --- |
| xunit v2 | GalaxyCheck.Xunit | `[Property]` | `[InlineData]`, `[MemberData]` |
| xunit v3 | GalaxyCheck.Xunit.V3 | `[Property]` | `[InlineData]`, `[MemberData]` |
| TUnit | GalaxyCheck.TUnit | `[Test, PropertyTest]` | `[PropertyArguments]` |
| NUnit | GalaxyCheck.NUnit | `[Property]` | `[TestCase]`, `[TestCaseSource]` |
| MSTest | GalaxyCheck.MSTest | `[Property]` | `[DataRow]`, `[DynamicData]` |

Each package builds for net10.0 and net11.0. Each package refers to the core package, GalaxyCheck. To use
GalaxyCheck with no test framework, read [Writing properties](writing-properties.md).

## Words used here

- Property: a statement that must be true for each value of a generator.
- Test case: one run of the property with one generated value.
- Data row: fixed values for the first parameters of a property method. GalaxyCheck generates the other
  parameters.

## The forms of a property method

GalaxyCheck generates each parameter of a property method with `Gen.Create<T>()`. A property method can return
one of these types:

| Return type | The test case fails when |
| --- | --- |
| `void` or `Task` | the method throws an exception. |
| `bool` or `Task<bool>` | the method returns false, or throws. |
| `Property` or `AsyncProperty` | the returned property fails. |
| `IGen<Property.Test<T>>` or `IGen<Property.AsyncTest<T>>` | the returned property fails. |

MSTest accepts only `void` and `Task`. Read [MSTest](#mstest).

## xunit v2

Add the package:

```shell
dotnet add package GalaxyCheck.Xunit
```

Then write a property:

```csharp
using GalaxyCheck;
using Xunit;

public class ReverseTests
{
    [Property]
    public bool ReverseTwiceGivesTheSameList(List<int> xs) =>
        xs.AsEnumerable().Reverse().Reverse().SequenceEqual(xs);

    [Property(Iterations = 200)]
    [InlineData(1)]
    [InlineData(-1)]
    public void AddThenSubtract(int delta, int x)
    {
        Assert.Equal(x, unchecked(x + delta - delta));
    }
}
```

Run the tests with `dotnet test`. Each `[InlineData]` row runs one property. The row gives the first parameter,
and GalaxyCheck generates the second parameter.

The package [GalaxyCheck.Xunit.CodeAnalysis](https://www.nuget.org/packages/GalaxyCheck.Xunit.CodeAnalysis/)
adds analyzers for GalaxyCheck.Xunit. The analyzers find a `[MemberGen]` that names a member that does not exist,
or a member of the wrong type.

## xunit v3

Add the package:

```shell
dotnet add package GalaxyCheck.Xunit.V3
```

An xunit v3 test project is an executable. This project file, with the package of GalaxyCheck added, runs with
`dotnet test`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsTestProject>true</IsTestProject>
    <IsTestingPlatformApplication>false</IsTestingPlatformApplication>
    <UseMicrosoftTestingPlatformRunner>false</UseMicrosoftTestingPlatformRunner>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.12.0" />
    <PackageReference Include="xunit.v3" Version="4.0.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
  </ItemGroup>

</Project>
```

The two `false` settings make `dotnet test` use the VSTest runner. Remove them to use the Microsoft Testing
Platform. The test code is the same as for xunit v2:

```csharp
using GalaxyCheck;

public class ReverseTestsV3
{
    [Property]
    public bool ReverseTwiceGivesTheSameList(List<int> xs) =>
        xs.AsEnumerable().Reverse().Reverse().SequenceEqual(xs);

    [Property]
    [InlineData("a")]
    [InlineData("")]
    public bool ConcatAddsTheLengths(string first, string second) =>
        (first + second).Length == first.Length + second.Length;
}
```

## TUnit

Add the packages:

```shell
dotnet add package TUnit
dotnet add package GalaxyCheck.TUnit
```

TUnit finds a test only through its own `[Test]` attribute, and TUnit has its own `[Property]` attribute. Thus,
a TUnit property has two attributes: `[Test, PropertyTest]`. `PropertyTest` has each option of `[Property]`.

```csharp
using GalaxyCheck;

public class ReverseTestsTUnit
{
    [Test, PropertyTest]
    public bool ReverseTwiceGivesTheSameList(List<int> xs) =>
        xs.AsEnumerable().Reverse().Reverse().SequenceEqual(xs);

    [Test, PropertyTest(Iterations = 50)]
    [PropertyArguments(1)]
    [PropertyArguments(2)]
    public bool MultiplyThenDivide(int factor, int x) =>
        (long)x * factor / factor == x;

    [Test, Sample]
    public void ShowTheValues(int x)
    {
    }
}
```

These are the rules for TUnit:

- A data row is `[PropertyArguments(...)]`. Each attribute is one row for the first parameters. TUnit refuses
  its own data rows when a row does not give each parameter.
- `[Sample]` and `[GenSnapshot]` also need `[Test]`: write `[Test, Sample]` and `[Test, GenSnapshot]`.
- The package keeps `PropertyAttribute`. To use `[Property]` in a file, add
  `using PropertyAttribute = GalaxyCheck.PropertyAttribute;` to that file.

TUnit tests run with `dotnet run`, or with `dotnet test` on the Microsoft Testing Platform.

## NUnit

Add the packages:

```shell
dotnet add package Microsoft.NET.Test.Sdk
dotnet add package NUnit
dotnet add package NUnit3TestAdapter
dotnet add package GalaxyCheck.NUnit
```

NUnit has its own `[Property]` attribute in `NUnit.Framework`. Thus, a file that uses `[Property]` of
GalaxyCheck does not import `NUnit.Framework`. Write the full name of each NUnit attribute in that file:

```csharp
using GalaxyCheck;

[NUnit.Framework.TestFixture]
public class ReverseTestsNUnit
{
    [Property]
    public bool ReverseTwiceGivesTheSameList(List<int> xs) =>
        xs.AsEnumerable().Reverse().Reverse().SequenceEqual(xs);

    [Property(Iterations = 50)]
    [NUnit.Framework.TestCase(1)]
    [NUnit.Framework.TestCase(2)]
    public bool MultiplyThenDivide(int factor, int x) =>
        (long)x * factor / factor == x;
}
```

These are the rules for NUnit:

- Put `[NUnit.Framework.TestFixture]` on each test class. NUnit loads a test assembly only when the assembly
  refers to NUnit, and the attribute makes that reference.
- A data row is NUnit's own `[TestCase(...)]` or `[TestCaseSource]`. A row can give only the first parameters,
  and GalaxyCheck generates the other parameters.

## MSTest

Add the package:

```shell
dotnet add package MSTest
dotnet add package GalaxyCheck.MSTest
```

MSTest runs only a test method that returns `void`, `Task`, or `ValueTask`. Thus, an MSTest property returns
`void` or `Task`, and it throws an exception to fail:

```csharp
using GalaxyCheck;

[Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
public class ReverseTestsMSTest
{
    [Property]
    public void ReverseTwiceGivesTheSameList(List<int> xs)
    {
        if (!xs.AsEnumerable().Reverse().Reverse().SequenceEqual(xs))
        {
            throw new Exception("The list changed.");
        }
    }

    [Property(Iterations = 50)]
    [Microsoft.VisualStudio.TestTools.UnitTesting.DataRow(1)]
    [Microsoft.VisualStudio.TestTools.UnitTesting.DataRow(2)]
    public async Task MultiplyThenDivide(int factor, int x)
    {
        await Task.Yield();
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(x, (int)((long)x * factor / factor));
    }
}
```

These are the rules for MSTest:

- Put `[TestClass]` on each test class.
- A method that returns `bool`, `Property`, or a generator is not a test in MSTest. To run a `Property`, call
  `.Assert()` on it in a `void` method.
- The MSTest package has no `[GenSnapshot]`.

## Choose the generator of a parameter

GalaxyCheck generates a parameter with `Gen.Create<T>()`. These attributes change the generator:

```csharp
using GalaxyCheck;
using GalaxyCheck.Gens.Injection.Int32;

public class ParameterTests
{
    public static IGen<string> ShortNames => Gen.String().WithLengthBetween(1, 5);

    [Property]
    public bool NamesAreShort([MemberGen(nameof(ShortNames))] string name) => name.Length <= 5;

    [Property]
    public bool DiceAreInRange([Between(1, 6)] int die) => die is >= 1 and <= 6;
}
```

`[MemberGen]` names a static property or field of the test class that holds an `IGen<T>`. A method is not a
member generator.

## Look at the values

Replace `[Property]` with `[Sample]` to see the values that a property gets. The test fails, and its message
lists the sampled values. In TUnit, write `[Test, Sample]`.

## Next steps

- [Writing properties](writing-properties.md) shows the `Property` API and the options of a run.
- [Configuration](configuration.md) sets the defaults of a test assembly.
- [The generator reference](generators.md) lists each generator.
