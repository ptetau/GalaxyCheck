# Generators

This page has one entry for each public generator method of `Gen` and of `Gen.Realistic`. Each entry has one C# example. Each example assumes `using GalaxyCheck;`.

A test of the library checks that each public generator method has an entry. When you add a generator method, add its entry here.

## Gen

### Gen.BFloat16

Generates `BFloat16` values (net11.0 only).

```csharp
var gen = Gen.BFloat16().WithoutSpecialValues();
```

### Gen.BigInteger

Generates `BigInteger` values.

```csharp
var gen = Gen.BigInteger().GreaterThanEqual(0);
```

### Gen.Boolean

Generates `true` and `false`.

```csharp
var gen = Gen.Boolean();
```

### Gen.Byte

Generates `byte` values.

```csharp
var gen = Gen.Byte().LessThanEqual(100);
```

### Gen.Char

Generates characters of the given types.

```csharp
var gen = Gen.Char(Gen.CharType.Alphabetical | Gen.CharType.Numeric);
```

### Gen.Choose

Picks one generator from a list, with an optional weight for each.

```csharp
var gen = Gen.Choose((Gen.Int32().LessThanEqual(0), 1), (Gen.Int32().GreaterThanEqual(100), 3));
```

### Gen.Complex

Generates `System.Numerics.Complex` values.

```csharp
var gen = Gen.Complex().WithoutSpecialValues();
```

### Gen.ComplexOf

Generates `Complex<T>` values from a generator of each part (net11.0 only).

```csharp
var gen = Gen.ComplexOf(Gen.Double().Between(-1, 1), Gen.Double().Between(-1, 1));
```

### Gen.Constant

Always generates the same value.

```csharp
var gen = Gen.Constant("fixed");
```

### Gen.Create

Generates values of a type. It reads the type by reflection, or uses the code of the source generator.

```csharp
public record Order(int Id, string Name);

var gen = Gen.Create<Order>();
```

### Gen.DateOnly

Generates `DateOnly` values.

```csharp
var gen = Gen.DateOnly().From(new DateOnly(2000, 1, 1));
```

### Gen.DateTime

Generates `DateTime` values.

```csharp
var gen = Gen.DateTime().From(new DateTime(2000, 1, 1)).Until(new DateTime(2030, 1, 1));
```

### Gen.DateTimeOffset

Generates `DateTimeOffset` values.

```csharp
var gen = Gen.DateTimeOffset().From(DateTimeOffset.UnixEpoch);
```

### Gen.Decimal

Generates `decimal` values.

```csharp
var gen = Gen.Decimal().Between(0m, 100m);
```

### Gen.Decimal128

Generates IEEE 754 `Decimal128` values (net11.0 only).

```csharp
var gen = Gen.Decimal128().WithoutSpecialValues();
```

### Gen.Decimal32

Generates IEEE 754 `Decimal32` values (net11.0 only).

```csharp
var gen = Gen.Decimal32().WithoutSpecialValues();
```

### Gen.Decimal64

Generates IEEE 754 `Decimal64` values (net11.0 only).

```csharp
var gen = Gen.Decimal64().WithoutSpecialValues();
```

### Gen.Dictionary

Generates dictionaries from a key generator and a value generator.

```csharp
var gen = Gen.Dictionary(Gen.String(), Gen.Int32()).WithCountLessThanEqual(5);
```

### Gen.Double

Generates `double` values, with the special values unless you remove them.

```csharp
var gen = Gen.Double().Between(-1.0, 1.0);
```

### Gen.Element

Picks one element of a collection.

```csharp
var gen = Gen.Element(new[] { "red", "green", "blue" });
```

### Gen.Email

Generates realistic email addresses.

```csharp
var gen = Gen.Email();
```

### Gen.Enum

Generates the values of an enum.

```csharp
var gen = Gen.Enum<DayOfWeek>();
```

### Gen.Factory

Makes a factory that shares type registrations between auto-generators.

```csharp
public record Order(int Id, string Name);

var gen = Gen.Factory().RegisterType(Gen.Int32().GreaterThanEqual(0)).Create<Order>();
```

### Gen.Function

Generates pure functions that return values of a generator.

```csharp
var gen = Gen.Function<int, bool>(Gen.Boolean());
```

### Gen.Guid

Generates `Guid` values.

```csharp
var gen = Gen.Guid();
```

### Gen.Half

Generates `Half` values.

```csharp
var gen = Gen.Half().WithoutSpecialValues();
```

### Gen.Infinite

Generates infinite sequences of elements.

```csharp
var gen = Gen.Infinite(Gen.Int32());
```

### Gen.Int128

Generates `Int128` values.

```csharp
var gen = Gen.Int128().GreaterThanEqual(0);
```

### Gen.Int16

Generates `short` values.

```csharp
var gen = Gen.Int16().GreaterThanEqual(-10).LessThanEqual(10);
```

### Gen.Int32

Generates `int` values.

```csharp
var gen = Gen.Int32().GreaterThanEqual(0).LessThanEqual(100);
```

### Gen.Int64

Generates `long` values.

```csharp
var gen = Gen.Int64().ShrinkTowards(1);
```

### Gen.IntPtr

Generates `nint` values.

```csharp
var gen = Gen.IntPtr().GreaterThanEqual(0);
```

### Gen.KeyValuePair

Generates key-value pairs from a key generator and a value generator.

```csharp
var gen = Gen.KeyValuePair(Gen.String(), Gen.Int32());
```

### Gen.List

Generates lists of elements.

```csharp
var gen = Gen.List(Gen.Int32()).WithCountLessThanEqual(10);
```

### Gen.Nullable

Generates `null` or a value of a reference type.

```csharp
var gen = Gen.Nullable(Gen.String());
```

### Gen.NullableStruct

Generates `null` or a value of a struct.

```csharp
var gen = Gen.NullableStruct(Gen.Int32());
```

### Gen.Parameters

Generates the arguments of a method.

```csharp
var method = typeof(Math).GetMethod(nameof(Math.Max), new[] { typeof(int), typeof(int) })!;
var gen = Gen.Parameters(method);
```

### Gen.Recursive

Generates values of a type that holds itself, up to a depth.

```csharp
var gen = Gen.Recursive(Gen.Constant("leaf"), inner => inner.Select(x => $"({x})"), maxDepth: 3);
```

### Gen.ReferenceRngWaypoint

Gives a generator its own random state, so that a change to other generators does not change its values.

```csharp
var gen = Gen.Int32().Advanced.ReferenceRngWaypoint(influence: 1);
```

### Gen.Rune

Generates Unicode scalar values.

```csharp
var gen = Gen.Rune();
```

### Gen.SByte

Generates `sbyte` values.

```csharp
var gen = Gen.SByte().GreaterThanEqual(0);
```

### Gen.Set

Generates sets of distinct elements.

```csharp
var gen = Gen.Set(Gen.Int32()).WithCountLessThanEqual(5);
```

### Gen.SetRngWaypoint

Starts a new random state for a generator.

```csharp
var gen = Gen.Int32().Advanced.SetRngWaypoint();
```

### Gen.Single

Generates `float` values.

```csharp
var gen = Gen.Single().Between(0f, 1f);
```

### Gen.String

Generates strings.

```csharp
var gen = Gen.String().WithLengthLessThanEqual(20);
```

### Gen.StringMatching

Generates strings that match a regular expression.

```csharp
var gen = Gen.StringMatching("[a-z]{3}-[0-9]{2}");
```

### Gen.TimeOnly

Generates `TimeOnly` values.

```csharp
var gen = Gen.TimeOnly().Until(new TimeOnly(12, 0));
```

### Gen.TimeSpan

Generates `TimeSpan` values.

```csharp
var gen = Gen.TimeSpan().Within(TimeSpan.Zero, TimeSpan.FromHours(1));
```

### Gen.Tuple

Generates `System.Tuple` values from a generator of each item.

```csharp
var gen = Gen.Tuple(Gen.Int32(), Gen.String());
```

### Gen.UInt128

Generates `UInt128` values.

```csharp
var gen = Gen.UInt128().LessThanEqual(1000);
```

### Gen.UInt16

Generates `ushort` values.

```csharp
var gen = Gen.UInt16().LessThanEqual(1000);
```

### Gen.UInt32

Generates `uint` values.

```csharp
var gen = Gen.UInt32().LessThanEqual(1000);
```

### Gen.UInt64

Generates `ulong` values.

```csharp
var gen = Gen.UInt64().LessThanEqual(1000);
```

### Gen.UIntPtr

Generates `nuint` values.

```csharp
var gen = Gen.UIntPtr().LessThanEqual(1000);
```

### Gen.Uri

Generates absolute URIs.

```csharp
var gen = Gen.Uri();
```

### Gen.ValueTuple

Generates value tuples from a generator of each item.

```csharp
var gen = Gen.ValueTuple(Gen.Int32(), Gen.Boolean());
```

### Gen.Version

Generates `System.Version` values.

```csharp
var gen = Gen.Version();
```

### Gen.Zip

Combines generators into a generator of tuples.

```csharp
var gen = Gen.Zip(Gen.Int32(), Gen.String());
```

## Gen.Realistic

### Gen.Realistic.City

Generates realistic city names.

```csharp
var gen = Gen.Realistic.City();
```

### Gen.Realistic.CompanyName

Generates realistic company names.

```csharp
var gen = Gen.Realistic.CompanyName();
```

### Gen.Realistic.Country

Generates realistic country names.

```csharp
var gen = Gen.Realistic.Country();
```

### Gen.Realistic.FirstName

Generates realistic first names.

```csharp
var gen = Gen.Realistic.FirstName();
```

### Gen.Realistic.FullName

Generates realistic full names.

```csharp
var gen = Gen.Realistic.FullName();
```

### Gen.Realistic.LastName

Generates realistic last names.

```csharp
var gen = Gen.Realistic.LastName();
```

### Gen.Realistic.PhoneNumber

Generates realistic phone numbers.

```csharp
var gen = Gen.Realistic.PhoneNumber();
```

### Gen.Realistic.StreetAddress

Generates realistic street addresses.

```csharp
var gen = Gen.Realistic.StreetAddress();
```
