# The source generator and NativeAOT

The package GalaxyCheck.SourceGeneration writes the code of `Gen.Create<T>()` at compile time. With the
generated code, `Gen.Create<T>()` reads no type by reflection at run time. Thus, a test project that is published
with trimming and with NativeAOT runs its properties. A GalaxyCheck package gives no trim warning and no AOT
warning.

For a type that both paths support, the generated code and the reflection path give the same values for the same
seed.

## Add the package

```shell
dotnet add package GalaxyCheck.SourceGeneration
```

The package holds only an analyzer and a source generator. It adds no assembly to your output. To refer to the
project in a clone of this repository, use an analyzer reference:

```xml
<ItemGroup>
  <ProjectReference Include="..\GalaxyCheck\GalaxyCheck.csproj" />
  <ProjectReference Include="..\GalaxyCheck.SourceGeneration\GalaxyCheck.SourceGeneration.csproj"
                    OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

No code change is necessary. Each call of `Gen.Create<T>()` with a known type uses the generated code:

```csharp
using GalaxyCheck;

var result = Property.ForAll(Gen.Create<Order>(), order => order.Name != null).Check(iterations: 20, seed: 0);
Console.WriteLine(result.Falsified ? "FAIL" : "PASS");

public record Order(int Id, string Name);
```

To check the app with NativeAOT, publish it:

```shell
dotnet publish -r linux-x64 -p:PublishAot=true
```

## Diagnostics

The package reports two warnings:

| Diagnostic | Meaning |
| --- | --- |
| GCSG001 | The source generator cannot write the code of a type. The warning names the type, and the call uses reflection. |
| GCSG002 | The call always uses reflection at run time. It does not work with trimming or NativeAOT. |

GCSG001 (no generated code for the type) occurs for these types:

- A collection.
- An abstract class.
- A type that refers to itself.
- A type with a `required` member.
- A file-local type.
- A generic type with a reference type argument.

GCSG002 (the call uses reflection) occurs for these calls:

- `Gen.Create<T>()` where `T` is a type parameter.
- `Create<T>()` on a factory with a registration, for example `Gen.Factory().RegisterType(...).Create<T>()`.
- Each `OverrideMember(...)`.
- `Gen.Create<T>(nullabilityInfo)` with a `NullabilityInfo`.

`Gen.Factory().Create<T>()` with no registration uses the generated code.

To keep an app free of reflection, change each call with a GCSG002 (the call uses reflection) warning. For example, replace
`OverrideMember` with a LINQ expression that builds the value from explicit generators.
