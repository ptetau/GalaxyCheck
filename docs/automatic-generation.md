# Automatic generation

`Gen.Create<T>()` builds a generator for a type from the generators of its parts. A `[Property]` parameter uses
the same generator. Thus, a property can take a record, a class, or a collection as a parameter with no extra
code.

## Words used here

- Factory: an `IGenFactory`, which holds registered generators for `Create<T>()`.
- Reflection path: the code that reads a type at run time to build its generator.
- Source generator: the package that writes the same code at compile time. Read
  [The source generator and NativeAOT](source-generator.md).

## The types that `Gen.Create<T>()` makes

- Each base type, with the generator of that type and no option. Read [Base types](base-types.md) and
  [Time types](time-types.md).
- `string`, `Uri`, and `Version`. Read [Text](text.md).
- Arrays, lists, sets, dictionaries, pairs, and tuples. Read [Containers](containers.md).
- `Nullable<T>`, and a nullable reference type.
- A record, or a class with a public constructor or settable properties.
- An interface or an abstract class. Read [Interfaces and abstract classes](#interfaces-and-abstract-classes).
- A type that refers to itself. No value is deeper than 5 levels.
- On net11.0, a C# union type. The generator makes each case of the union.

```csharp
using GalaxyCheck;

var orders = Gen.Create<Order>();

Property.ForAll(orders, order => order.Lines != null).Assert();
Console.WriteLine(orders.SampleOne(seed: 0).GetType().Name);

record Order(Guid Id, string Customer, List<OrderLine> Lines, DateTimeOffset Placed);
record OrderLine(string Sku, int Quantity, decimal Price);
```

## Change the generator of a member

`OverrideMember` sets the generator of one member. A factory registers a generator for each member of a type:

```csharp
using GalaxyCheck;

var lines = Gen.Create<OrderLine>()
    .OverrideMember(line => line.Quantity, Gen.Int32().Between(1, 9));

var cheapLines = Gen.Factory()
    .RegisterType(Gen.Decimal().Between(0m, 10m))
    .Create<OrderLine>();

Property.ForAll(lines, line => line.Quantity is >= 1 and <= 9).Assert();
Property.ForAll(cheapLines, line => line.Price is >= 0m and <= 10m).Assert();
Console.WriteLine("Both properties hold.");

record OrderLine(string Sku, int Quantity, decimal Price);
```

In a test class, an attribute gives a factory or a generator:

- A subclass of `GenFactoryAttribute` on a method or a class gives the factory of each parameter.
- A subclass of `GenAttribute` on a parameter gives the generator of that parameter.
- `[Between(x, y)]`, `[GreaterThanEqual(min)]`, and `[LessThanEqual(max)]` in the namespace
  `GalaxyCheck.Gens.Injection.Int32` bound an `int` parameter.

```csharp
using GalaxyCheck;
using GalaxyCheck.Gens;

public class SmallNumbers : GenFactoryAttribute
{
    public override IGenFactory Value => Gen.Factory().RegisterType(Gen.Int32().Between(0, 9));
}

public class FactoryTests
{
    [Property]
    [SmallNumbers]
    public bool EachIntIsSmall(int x, List<int> xs) => x <= 9 && xs.All(y => y <= 9);
}
```

## Interfaces and abstract classes

For an interface or an abstract class, `Gen.Create<T>()` makes instances of the concrete types in the assembly of
`T` that are assignable to `T`. Each such type appears in the values. With no such type, the result is a
generator error that names `T`.

To give the list of concrete types yourself, use `RegisterImplementations`:

```csharp
using GalaxyCheck;

var anyShape = Gen.Create<IShape>();
var onlyCircles = Gen.Factory().RegisterImplementations<IShape>(typeof(Circle)).Create<IShape>();

Console.WriteLine(string.Join(", ", anyShape.Sample(iterations: 20, seed: 0).Select(s => s.GetType().Name).Distinct()));
Property.ForAll(onlyCircles, shape => shape is Circle).Assert();

interface IShape { }
record Circle(int Radius) : IShape;
record Square(int Side) : IShape;
```

The first line of the output names both `Circle` and `Square`.
