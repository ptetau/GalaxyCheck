using GalaxyCheck;

foreach (var order in Gen.Create<Order>().Sample(iterations: 100, seed: 0))
{
    Console.WriteLine($"{order.Id}|{order.Name.Length}|{System.Text.Json.JsonSerializer.Serialize(order.Name)}");
}

public record Order(int Id, string Name);
