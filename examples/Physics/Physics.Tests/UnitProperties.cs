using GalaxyCheck;

namespace Physics.Tests;

public class UnitProperties
{
    // The bound keeps each result finite: the largest ratio of two units (light years to centimetres) is about 1e18.
    public static IGen<double> Distance => PhysicsGen.Between(-1e200, 1e200);

    // A result below about 2e-308 is subnormal: it has fewer significant bits, so it gets an absolute tolerance.
    private static bool Close(double expected, double actual, double relative) =>
        Math.Abs(expected - actual) <= relative * Math.Abs(expected) + 1e-300;

    // Round trip: to convert and convert back gives the start value, to a few units in the last place.
    [Test, PropertyTest]
    public bool LengthConversionRoundTrips([MemberGen(nameof(Distance))] double value, LengthUnit from, LengthUnit to) =>
        Close(value, Units.Convert(Units.Convert(value, from, to), to, from), 1e-14);

    // A round trip cannot find a wrong factor that both directions share. Known facts can.
    [Test, PropertyTest]
    public bool TwelveInchesMakeOneFoot([MemberGen(nameof(Distance))] double feet) =>
        Close(feet, Units.Convert(feet * 12, LengthUnit.Inch, LengthUnit.Foot), 1e-14);

    [Test, PropertyTest]
    public bool OneMileIs5280Feet([MemberGen(nameof(Distance))] double miles) =>
        Close(miles * 5280, Units.Convert(miles, LengthUnit.Mile, LengthUnit.Foot), 1e-14);

    // Metamorphic: a detour through a third unit gives the same result as the direct conversion.
    [Test, PropertyTest]
    public bool ConversionThroughAThirdUnitAgrees(
        [MemberGen(nameof(Distance))] double value, LengthUnit from, LengthUnit via, LengthUnit to) =>
        Close(Units.Convert(value, from, to), Units.Convert(Units.Convert(value, from, via), via, to), 1e-14);

    // Metamorphic: conversion never swaps the order of two values. Two values can become equal, because a
    // subnormal value such as 5e-324 underflows to 0.
    [Test, PropertyTest]
    public bool ConversionNeverSwapsTheOrder(
        [MemberGen(nameof(Distance))] double a, [MemberGen(nameof(Distance))] double b, LengthUnit from, LengthUnit to)
    {
        var (low, high) = a <= b ? (a, b) : (b, a);
        return Units.Convert(low, from, to) <= Units.Convert(high, from, to);
    }

    public static IGen<double> Special => Gen.Element(new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, 0.0 });

    [Test, PropertyTest]
    public bool SpecialValuesPassThroughConversion([MemberGen(nameof(Special))] double value, LengthUnit from, LengthUnit to) =>
        Units.Convert(value, from, to).Equals(value);

    // From absolute zero up to the core of the sun.
    public static IGen<double> Kelvin => PhysicsGen.Between(0, 2e7);

    [Test, PropertyTest]
    public bool TemperatureConversionRoundTrips(
        [MemberGen(nameof(Kelvin))] double kelvin, TemperatureUnit from, TemperatureUnit to)
    {
        var value = Units.Convert(kelvin, TemperatureUnit.Kelvin, from);
        var back = Units.Convert(Units.Convert(value, from, to), to, from);
        return Math.Abs(back - value) <= 1e-9 * Math.Max(1, Math.Abs(value));
    }

    public static IGen<double> Fahrenheit => PhysicsGen.Between(-459.67, 1e6);

    // Known edge cases run before the generated values: absolute zero, where rounding can give a value a little below
    // 0 K, and -40, where both scales meet.
    [Test, PropertyTest]
    [Example(-459.67)]
    [Example(-40.0)]
    public bool FahrenheitToCelsiusFollowsTheDefinition([MemberGen(nameof(Fahrenheit))] double fahrenheit)
    {
        var expected = (fahrenheit - 32) * 5 / 9;
        var actual = Units.Convert(fahrenheit, TemperatureUnit.Fahrenheit, TemperatureUnit.Celsius);
        return Math.Abs(actual - expected) <= 1e-9 * Math.Max(1, Math.Abs(expected));
    }

    public static IGen<double> BelowAbsoluteZero => PhysicsGen.Between(-1e6, -1e-6);

    [Test, PropertyTest]
    public void TemperatureBelowAbsoluteZeroIsRejected([MemberGen(nameof(BelowAbsoluteZero))] double kelvinBelowZero, TemperatureUnit from, TemperatureUnit to)
    {
        var value = Units.Convert(0, TemperatureUnit.Kelvin, from) + kelvinBelowZero * (from == TemperatureUnit.Fahrenheit ? 1.8 : 1);
        try
        {
            Units.Convert(value, from, to);
        }
        catch (ArgumentOutOfRangeException)
        {
            return;
        }

        throw new InvalidOperationException($"{value} {from} was accepted.");
    }
}
