using System.Collections.Immutable;
using GalaxyCheck;
using GalaxyCheck.Stateful;

namespace Banking.Tests;

/// <summary>
/// A model-based test. The model of the bank is a dictionary of balances and overdraft limits, simple enough to be
/// obviously right. GalaxyCheck runs random command sequences on a real <see cref="Bank"/> and on the model, and
/// fails when the two disagree. A failure shows the shortest command sequence that it found.
/// </summary>
public class BankModelTests
{
    [Property]
    public Property BankAgreesWithTheModel() =>
        StatefulProperty.Create(() => new Bank(), () => BankModel.Empty, NextCommand);

    private static readonly string[] Ids = ["alice", "bob", "carol"];

    private static IGen<Command<Bank, BankModel>> NextCommand(BankModel model)
    {
        var open =
            from id in Gen.Element(Ids)
            from limit in Gen.Element(new[] { 0m, 50m, 200m })
            select (Command<Bank, BankModel>)new OpenAccount(id, limit);

        if (model.Accounts.IsEmpty)
        {
            return open;
        }

        var opened = model.Accounts.Keys.Order().ToList();
        var deposit =
            from id in Gen.Element(opened)
            from amount in BankingGen.UsdAmount
            select (Command<Bank, BankModel>)new Deposit(id, amount);
        var withdraw =
            from id in Gen.Element(opened)
            from amount in BankingGen.UsdAmount
            select (Command<Bank, BankModel>)new Withdraw(id, amount);
        var transfer =
            from payer in Gen.Element(opened)
            from payee in Gen.Element(opened)
            from amount in BankingGen.UsdAmount
            where payer != payee
            select (Command<Bank, BankModel>)new Transfer(payer, payee, amount);

        var choices = Gen.Choose<Command<Bank, BankModel>>()
            .WithChoice(open, 1)
            .WithChoice(deposit, 3)
            .WithChoice(withdraw, 3);
        return opened.Count > 1 ? choices.WithChoice(transfer, 4) : choices;
    }
}

public sealed record ModelAccount(decimal Balance, decimal Limit)
{
    public decimal Available => Balance + Limit;
}

public sealed record BankModel(ImmutableDictionary<string, ModelAccount> Accounts)
{
    public static BankModel Empty { get; } = new(ImmutableDictionary<string, ModelAccount>.Empty);

    public BankModel Add(string id, decimal amount) =>
        new(Accounts.SetItem(id, Accounts[id] with { Balance = Accounts[id].Balance + amount }));

    /// <summary>True if the bank agrees with each balance of the model.</summary>
    public bool Matches(Bank bank) => Accounts.All(a => bank.Balance(a.Key).Amount == a.Value.Balance);
}

public abstract class BankCommand : Command<Bank, BankModel>
{
    public override bool Postcondition(Bank system, BankModel modelBefore, BankModel modelAfter) => modelAfter.Matches(system);

    // A refused payment is a correct result when the model also refuses it. The postcondition then checks that
    // the bank changed no balance.
    public override bool Expects(Exception exception) => exception is InsufficientFundsException;
}

public sealed class OpenAccount(string id, decimal limit) : BankCommand
{
    public override bool Precondition(BankModel model) => !model.Accounts.ContainsKey(id);

    public override void Run(Bank system) => system.Open(id, Currency.Usd, limit);

    public override BankModel NextModel(BankModel model) => new(model.Accounts.Add(id, new ModelAccount(0m, limit)));

    public override string ToString() => $"Open {id} with limit {limit}";
}

public sealed class Deposit(string id, Money amount) : BankCommand
{
    public override void Run(Bank system) => system.Deposit(id, amount);

    public override BankModel NextModel(BankModel model) => model.Add(id, amount.Amount);

    public override string ToString() => $"Deposit {amount} to {id}";
}

public sealed class Withdraw(string id, Money amount) : BankCommand
{
    public override void Run(Bank system) => system.Withdraw(id, amount);

    public override BankModel NextModel(BankModel model) =>
        amount.Amount <= model.Accounts[id].Available ? model.Add(id, -amount.Amount) : model;

    public override string ToString() => $"Withdraw {amount} from {id}";
}

public sealed class Transfer(string from, string to, Money amount) : BankCommand
{
    public override void Run(Bank system) => system.Transfer(from, to, amount);

    public override BankModel NextModel(BankModel model) =>
        amount.Amount <= model.Accounts[from].Available ? model.Add(from, -amount.Amount).Add(to, amount.Amount) : model;

    public override string ToString() => $"Transfer {amount} from {from} to {to}";
}
