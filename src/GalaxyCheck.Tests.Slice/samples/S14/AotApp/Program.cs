using GalaxyCheck;

var result = Property.ForAll(Gen.Create<Order>(), order => order.Id == order.Id && order.Name != null)
    .Check(iterations: 20, seed: 0);

if (result.Falsified)
{
    Console.WriteLine("FAIL");
    return 1;
}

Console.WriteLine("PASS");
return 0;

public record Order(int Id, string Name);
