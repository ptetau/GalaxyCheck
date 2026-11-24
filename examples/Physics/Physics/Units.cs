namespace Physics;

public enum LengthUnit
{
    Metre,
    Kilometre,
    Centimetre,
    Inch,
    Foot,
    Mile,
    NauticalMile,
    AstronomicalUnit,
    LightYear,
}

public enum TemperatureUnit
{
    Kelvin,
    Celsius,
    Fahrenheit,
}

/// <summary>Conversions between units. NaN stays NaN and an infinity stays an infinity of the same sign.</summary>
public static class Units
{
    public static double MetresPer(LengthUnit unit) => unit switch
    {
        LengthUnit.Metre => 1.0,
        LengthUnit.Kilometre => 1_000.0,
        LengthUnit.Centimetre => 0.01,
        LengthUnit.Inch => 0.0254,
        LengthUnit.Foot => 0.3048,
        LengthUnit.Mile => 1_609.344,
        LengthUnit.NauticalMile => 1_852.0,
        LengthUnit.AstronomicalUnit => 149_597_870_700.0,
        LengthUnit.LightYear => 9_460_730_472_580_800.0,
        _ => throw new ArgumentOutOfRangeException(nameof(unit)),
    };

    public static double Convert(double value, LengthUnit from, LengthUnit to) =>
        from == to ? value : value * (MetresPer(from) / MetresPer(to));

    public const double AbsoluteZeroCelsius = -273.15;

    /// <summary>Converts a temperature. A temperature below absolute zero is an error.</summary>
    public static double Convert(double value, TemperatureUnit from, TemperatureUnit to)
    {
        var kelvin = from switch
        {
            TemperatureUnit.Kelvin => value,
            TemperatureUnit.Celsius => value - AbsoluteZeroCelsius,
            TemperatureUnit.Fahrenheit => (value - 32.0) * 5.0 / 9.0 - AbsoluteZeroCelsius,
            _ => throw new ArgumentOutOfRangeException(nameof(from)),
        };

        // A value a rounding error below zero is absolute zero itself.
        if (kelvin < -1e-9)
        {
            throw new ArgumentOutOfRangeException(nameof(value), $"{value} {from} is below absolute zero.");
        }

        kelvin = Math.Max(kelvin, 0);
        return to switch
        {
            TemperatureUnit.Kelvin => kelvin,
            TemperatureUnit.Celsius => kelvin + AbsoluteZeroCelsius,
            TemperatureUnit.Fahrenheit => (kelvin + AbsoluteZeroCelsius) * 9.0 / 5.0 + 32.0,
            _ => throw new ArgumentOutOfRangeException(nameof(to)),
        };
    }
}
