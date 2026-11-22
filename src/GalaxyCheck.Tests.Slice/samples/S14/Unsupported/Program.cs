using GalaxyCheck;

var gen = Gen.Create<Unsupported>();
Console.WriteLine(gen);

public delegate int Callback(int x);

public sealed class Unsupported
{
    private Unsupported(Callback callback)
    {
        Callback = callback;
    }

    public Callback Callback { get; }
}
