# Stateful testing

A stateful test runs a sequence of commands against a real system and against a simple model of that system.
After each command, the test compares the system with the model. GalaxyCheck generates the sequences, and it
shrinks a failing sequence to a short sequence that still fails.

## Words used here

- System: the object under test, for example a cache or a queue.
- Model: a simple value that holds the expected state of the system.
- Command: one step of the test. A command runs an action on the system, and it gives the next model.
- Precondition: a condition on the model. A command runs only when its precondition is true.
- Postcondition: a condition on the system and the model after a command. A false postcondition fails the test.

## Write the commands

A command is a subclass of `Command<TSystem, TModel>` in the namespace `GalaxyCheck.Stateful`:

| Member | Use |
| --- | --- |
| `Precondition(model)` | True when the command can run in this state. The default is true. |
| `Run(system)` | Runs the action on the real system. |
| `NextModel(model)` | Returns the model after the command. It returns a new model and does not change its argument. |
| `Postcondition(system, modelBefore, modelAfter)` | True when the system agrees with the model. The default is true. |
| `Expects(exception)` | True when `Run` can throw this exception. The default is false. |
| `ToString()` | The text of the command in the failure report. |

## Make the property

`StatefulProperty.Create(newSystem, newModel, nextCommand)` makes a property. `nextCommand` takes the current
model and returns a generator of the next command. Each run makes a new system and a new model.

This counter has a bug: it adds 2 when its value is 3.

```csharp
using GalaxyCheck;
using GalaxyCheck.Stateful;

var property = StatefulProperty.Create(
    () => new Counter(),
    () => 0,
    model => Gen.Element(new Command<Counter, int>[] { new Increment(), new Read() }));

try
{
    property.Assert(seed: 0);
}
catch (GalaxyCheck.Runners.PropertyFailedException error)
{
    Console.WriteLine(error.Message);
}

class Counter
{
    public int Value { get; private set; }
    public void Increment() => Value += Value == 3 ? 2 : 1;
}

class Increment : Command<Counter, int>
{
    public override void Run(Counter system) => system.Increment();
    public override int NextModel(int model) => model + 1;
    public override string ToString() => "Increment";
}

class Read : Command<Counter, int>
{
    public override void Run(Counter system) { }
    public override int NextModel(int model) => model;
    public override bool Postcondition(Counter system, int modelBefore, int modelAfter) => system.Value == modelAfter;
    public override string ToString() => "Read";
}
```

The message of this program starts with these lines:

```
Falsified after 45 tests (4 shrinks)
Reproduction: AgAsCwEAAQABAAEAAQEA
Counterexample: [Increment, Increment, Increment, Increment, Read]
Counterexample as C#:
    [Increment, Increment, Increment, Increment, Read]

---- The postcondition of command 5 failed.
Commands:
1. Increment
2. Increment
3. Increment
4. Increment
5. Read <- failed
```

The report lists the commands of the shrunk sequence in order, and it marks the command that failed.

## The rules of a run

- The property fails when a postcondition is false, or when `Run` throws an exception that `Expects` does not
  accept.
- When `Expects` accepts the exception, the run goes on: the model moves to `NextModel`, and the postcondition is
  checked.
- The generator and the shrinker place a command only where its precondition is true. A shrunk sequence also
  shrinks the inputs of each command.
- `StatefulGen.Commands(newModel, nextCommand)` gives only the generator of command sequences, for your own
  property.

GalaxyCheck does not run commands in parallel.
