# Physics demo

This demo tests numerical physics code with GalaxyCheck and TUnit. The domain has vectors, unit conversion,
motion with a constant acceleration, elastic collisions, and two integrators (a spring and an orbit). Floating-point code seldom gives the exact answer. Thus each property here states a rule with a tolerance, and each
generator states its bounds.

## The domain

The project `Physics` holds the domain code:

- `Vector3.cs`: a vector in three dimensions. `Length` scales the components first, so that a vector of size 1e300
  or 1e-300 does not overflow or underflow.
- `Units.cs`: conversion of lengths (metres to light years) and of temperatures. A temperature below absolute zero
  is an error.
- `Mechanics.cs`: `Kinematics` (position, velocity, and the time to reach a distance), `Collision.Elastic` (two
  spheres that touch), `Spring` (velocity Verlet), and `Orbit` (leapfrog around a central mass).

## Run the tests

TUnit runs on Microsoft.Testing.Platform. The file `global.json` in this folder tells `dotnet test` to use it. Thus,
run the tests from this folder:

```
cd examples/Physics
dotnet test --project Physics.Tests
```

To see the spread of the generated vectors, run the report test:

```
dotnet test --project Physics.Tests -- --treenode-filter "/*/*/VectorProperties/ShowTheSpreadOfTheVectors" --output detailed
```

The output of one run:

```
Passed after 300 tests
Labels:
    48.3% has a zero component
    28% huge (above 1e150)
    24.3% tiny (below 1e-150)
    2.7% everyday (1e-10 to 1e10)
```

## The properties

Each line gives the property and the fault that it finds.

`VectorProperties.cs`:

- `NormalizedVectorHasLengthOne` (with cover): a length that overflows for large vectors or underflows for small
  ones. The cover rules fail the run if less than 10% of the vectors are tiny, or less than 10% are huge.
- `LengthLiesBetweenTheLargestComponentAndRootThreeTimesIt`: the same overflow and underflow, from a second rule.
- `SpecialComponentsGiveTheExpectedLength`: a NaN that becomes a number, or an infinity that becomes NaN.
- `CrossProductIsPerpendicularToBothInputs` (invariant): a sign or index error in the cross product.
- `CrossProductIsAntiSymmetric` (symmetry): the same errors, with an exact check.

`UnitProperties.cs`:

- `LengthConversionRoundTrips` (round trip): a conversion that is not the inverse of the conversion back.
- `TwelveInchesMakeOneFoot` and `OneMileIs5280Feet`: a wrong factor. A round trip cannot find a wrong factor,
  because the factor is wrong in both directions.
- `ConversionThroughAThirdUnitAgrees` (metamorphic): factors that do not agree with each other.
- `ConversionNeverSwapsTheOrder` (metamorphic): a negative factor, or an offset where none belongs.
- `SpecialValuesPassThroughConversion`: NaN, an infinity, or zero that the conversion changes.
- `TemperatureConversionRoundTrips` (round trip): a wrong offset or factor of Celsius or Fahrenheit.
- `FahrenheitToCelsiusFollowsTheDefinition` (with `[Example]` rows): a check of absolute zero with no tolerance for
  rounding. The examples run first: -459.67 °F, the double just below it, and -40 °F.
- `TemperatureBelowAbsoluteZeroIsRejected`: a missing check of absolute zero.

`MechanicsProperties.cs`:

- `TimeToReachIsTheInverseOfPosition` (inverse, with cover): the textbook quadratic formula, which divides by zero
  when the acceleration is zero and loses precision when it is small.
- `MomentumIsConserved`: an impulse with the wrong mass.
- `KineticEnergyIsConserved`: an impulse with the wrong size.
- `SwappingTheBodiesSwapsTheResult` (symmetry): code that treats the first body differently from the second.
- `CollisionLooksTheSameFromAMovingFrame` (metamorphic): a result that depends on absolute velocities, not relative
  ones.
- `BodiesThatMoveApartKeepTheirVelocities` (precondition): a collision of spheres that already move apart.
- `SpringEnergyStaysWithinOnePercentOverManySteps`: an integrator that gains energy, for example explicit Euler.
- `SpringFollowsTheExactSolution` (oracle): an integrator with the wrong phase or amplitude, against the exact
  solution x0 cos(wt) + (v0/w) sin(wt).
- `OrbitKeepsItsAngularMomentum` and `OrbitEnergyStaysNearItsStartValue`: an orbit integrator that is not
  symplectic.

## Patterns to copy

- **Clear bounds.** `PhysicsGen.cs` names the range of each generator. `Gen.Double()` without bounds also gives
  NaN and the infinities. Give those values their own property, as `SpecialComponentsGiveTheExpectedLength` does.
- **A wide scale.** `AnyScaleVector` multiplies three components of unit scale by a power of 10 from 1e-300 to 1e299.
  `WithBias(Gen.Bias.None)` gives each exponent the same chance. With the default bias, the first exponents stay
  near 0.
- **Zero gets its own share.** `Gen.Choose((Gen.Constant(0.0), 1), (..., 4))` makes 0 frequent. A range of doubles
  seldom hits exactly 0 by chance.
- **Cover and Label.** `.Cover("huge (above 1e150)", 10, ...)` fails the run if the inputs miss a class.
  `.Label(...)` only counts the class. `result.Report` lists the share of each label.
- **Tolerances.** Use a relative tolerance, plus a small absolute part for results near 0. A subnormal result has
  fewer significant bits.

## What the first runs found

The first runs of these properties found real faults in the domain code and in the tests:

- `Kinematics.TimeToReach` returned infinity for a speed of 6.27e-322, because the time overflowed. The code now
  returns null for a time that is not finite.
- `TimeToReach` returned null for a body at rest with an acceleration of 2.2e-16 and a distance of 5e-324. The
  general formula squared the tiny values and got 0. The code now has its own formula for a start from rest.
- `ConversionKeepsTheOrder` was false for 5e-324 and 0: a conversion to metres underflows 5e-324 to 0. The property
  now says that a conversion does not swap the order of two values. That is the true rule.
- `Gen.Double().Between(-100, 100)` gives subnormal values such as 5e-324 on purpose. Thus the tolerances of the
  cross product and of the unit conversions now have a small absolute part.

## A true failure

To make this failure, we put the textbook quadratic formula into `Kinematics.TimeToReach` in a scratch copy:

```csharp
return Finite((-v0 + Math.Sqrt(discriminant)) / a);   // was: Finite(2 * distance / (v0 + Math.Sqrt(discriminant)))
```

`dotnet test --project Physics.Tests` gave this output:

```
failed TimeToReachIsTheInverseOfPosition (118ms)
  [Test Failure] PropertyFailedException:

Falsified after 8 tests (6 shrinks)
Reproduction:
    [Replay("ArHbk9QGBwoAAAABAQAAAAAA")]
Counterexample:
    [0] = 1
    [1] = 0.001
    [2] = 0
Counterexample as C#:
    (1.0, 0.001, 0.0)

Property function returned false

<galaxycheck-failure>
{"counterexample":["(Item1 = 1, Item2 = 0.001, Item3 = 0)"],"csharp":["(1.0, 0.001, 0.0)"],"replay":"ArHbk9QGBwoAAAABAQAAAAAA","seed":1787096497,"size":7,"iterations":8,"shrinks":6,"test":"Physics.Tests.KinematicsProperties.TimeToReachIsTheInverseOfPosition","rerun":"dotnet run -f net10.0 -- --treenode-filter /Physics.Tests/Physics.Tests/KinematicsProperties/TimeToReachIsTheInverseOfPosition"}
</galaxycheck-failure>
```

The counterexample is a distance of 1 m, a speed of 0.001 m/s, and an acceleration of 0. The textbook formula
divides by the acceleration, so it gives no time for a body at constant speed. The `rerun` command runs only this
test. Run it in the folder `Physics.Tests`. The failure memory then tries the saved case first, so the second run
fails after 1 test.

## Mutations that the properties find

Each bug went into a scratch copy only. No bug is in the code of this folder.

| Bug in the domain code | Properties that failed |
| --- | --- |
| The textbook quadratic formula in `TimeToReach` | `TimeToReachIsTheInverseOfPosition` |
| `Length` without scaling: `Math.Sqrt(X * X + Y * Y + Z * Z)` | `NormalizedVectorHasLengthOne`, `LengthLiesBetweenTheLargestComponentAndRootThreeTimesIt`, `SpecialComponentsGiveTheExpectedLength` |
| The impulse on body A uses the mass of A, not of B | `MomentumIsConserved`, `KineticEnergyIsConserved`, `SwappingTheBodiesSwapsTheResult` |
| Explicit Euler in `Spring.Step` | `SpringEnergyStaysWithinOnePercentOverManySteps`, `SpringFollowsTheExactSolution` |
| No tolerance in the check of absolute zero | `FahrenheitToCelsiusFollowsTheDefinition`, at the row `[Example(-459.6700000000001)]` |

## Words used here

- **Subnormal**: a double below about 2.2e-308. It has fewer significant bits than a normal double. The smallest is
  5e-324.
- **Symplectic integrator**: a step method that keeps the energy of an orbit or a spring near its start value over
  long runs.
- **Cover**: a rule that fails the run when too few inputs have a label.
- **Invariant**: a rule that holds for each input, for example that a cross product is at right angles to its inputs.
- **Precondition**: a condition that a test case must meet. GalaxyCheck discards a case that does not meet it.
