using GalaxyCheck;

var values = Gen.Create<Pet>().Sample(iterations: 100, seed: 0);
var cases = values.Select(pet => pet.Value?.GetType().Name ?? "null").Distinct().OrderBy(name => name, StringComparer.Ordinal);
Console.WriteLine("CASES " + string.Join(",", cases));

public record Cat(int Lives);

public record Dog(int Age);

public record Bird(bool Flies);

public union Pet(Cat, Dog, Bird);
