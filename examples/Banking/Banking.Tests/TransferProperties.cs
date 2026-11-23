using GalaxyCheck;
using GalaxyCheck.Gens.Injection.Int32;

namespace Banking.Tests;

public class TransferProperties
{
    private static Bank BankWith(int aliceCents, int aliceLimitCents)
    {
        var bank = new Bank();
        bank.Open("alice", Currency.Usd, aliceLimitCents / 100m);
        bank.Open("bob", Currency.Usd);
        if (aliceCents > 0)
        {
            bank.Deposit("alice", Money.FromMinorUnits(aliceCents, Currency.Usd));
        }

        return bank;
    }

    // Precondition: only the transfers that the funds allow. The run discards the other cases.
    [Property]
    public void TransferWithinAvailableFundsMovesTheExactAmount(
        [Between(0, 100_000)] int aliceCents,
        [Between(0, 50_000)] int limitCents,
        [Between(1, 100_000)] int amountCents)
    {
        Property.Precondition(amountCents <= aliceCents + limitCents);
        var bank = BankWith(aliceCents, limitCents);

        bank.Transfer("alice", "bob", Money.FromMinorUnits(amountCents, Currency.Usd));

        Assert.Equal(Money.FromMinorUnits(aliceCents - amountCents, Currency.Usd), bank.Balance("alice"));
        Assert.Equal(Money.FromMinorUnits(amountCents, Currency.Usd), bank.Balance("bob"));
    }

    [Property]
    public void TransferAboveAvailableFundsFailsAndChangesNothing(
        [Between(0, 100_000)] int aliceCents,
        [Between(0, 50_000)] int limitCents,
        [Between(1, 200_000)] int amountCents)
    {
        Property.Precondition(amountCents > aliceCents + limitCents);
        var bank = BankWith(aliceCents, limitCents);
        var journalBefore = bank.Ledger.Journal.Count;

        Assert.Throws<InsufficientFundsException>(() =>
            bank.Transfer("alice", "bob", Money.FromMinorUnits(amountCents, Currency.Usd)));

        Assert.Equal(Money.FromMinorUnits(aliceCents, Currency.Usd), bank.Balance("alice"));
        Assert.Equal(journalBefore, bank.Ledger.Journal.Count);
    }

    public static IGen<IReadOnlyList<(int From, int To, Money Amount)>> TransferAttempts =>
        (from payer in Gen.Int32().Between(0, 2)
         from payee in Gen.Int32().Between(0, 2)
         from amount in BankingGen.UsdAmount
         select (payer, payee, amount)).ListOf().WithCountLessThanEqual(30);

    // Invariant: transfers move money between customers. They never create or destroy it, also when some fail.
    [Property]
    public void TransfersKeepTheCustomerTotal(
        [MemberGen(nameof(TransferAttempts))] IReadOnlyList<(int From, int To, Money Amount)> attempts)
    {
        string[] ids = ["alice", "bob", "carol"];
        var bank = new Bank();
        foreach (var id in ids)
        {
            bank.Open(id, Currency.Usd, overdraftLimit: 100m);
            bank.Deposit(id, Money.Of(250m, Currency.Usd));
        }

        var before = bank.CustomerTotal(Currency.Usd);
        foreach (var (from, to, amount) in attempts)
        {
            try
            {
                bank.Transfer(ids[from], ids[to], amount);
            }
            catch (Exception ex) when (ex is InsufficientFundsException or ArgumentException)
            {
                // A refused transfer is a normal result here.
            }
        }

        Assert.Equal(before, bank.CustomerTotal(Currency.Usd));
        Assert.All(ids, id => Assert.True(bank.Available(id).Amount >= 0, $"{id} is past its overdraft limit"));
    }

    // Invariant of double entry: the running balance of each account equals the sum of its journal postings,
    // and the balances of all accounts, internal ones included, add up to zero.
    [Property]
    public void RunningBalancesAgreeWithTheJournal(
        [MemberGen(nameof(TransferAttempts))] IReadOnlyList<(int From, int To, Money Amount)> attempts)
    {
        var bank = new Bank();
        bank.Open("alice", Currency.Usd, overdraftLimit: 300m);
        bank.Open("bob", Currency.Usd);
        bank.Open("carol", Currency.Usd);
        string[] ids = ["alice", "bob", "carol"];
        foreach (var (from, to, amount) in attempts)
        {
            if (from == to)
            {
                bank.Deposit(ids[to], amount);
            }
            else if (bank.Available(ids[from]) >= amount)
            {
                bank.Transfer(ids[from], ids[to], amount);
            }
        }

        var accounts = bank.Ledger.Accounts.ToList();
        Assert.All(accounts, a => Assert.Equal(bank.Ledger.BalanceFromJournal(a.Id), bank.Ledger.Balance(a.Id)));
        Assert.Equal(0m, accounts.Sum(a => bank.Ledger.Balance(a.Id).Amount));
    }

    public static IGen<InterestRates> Rates =>
        from credit in Gen.Int32().Between(0, 1_000)
        from overdraft in Gen.Int32().Between(0, 3_000)
        select new InterestRates(credit / 100m, overdraft / 100m);

    public static IGen<Money> Balances => BankingGen.Money;

    [Property]
    public bool InterestHasTheSignOfTheBalance(
        [MemberGen(nameof(Balances))] Money balance,
        [MemberGen(nameof(Rates))] InterestRates rates,
        [Between(0, 366)] int days)
    {
        var sign = Math.Sign(rates.Accrue(balance, days).Amount);
        return sign == 0 || sign == Math.Sign(balance.Amount);
    }

    // Metamorphic: interest for two periods in a row is the same as for one long period, except for rounding.
    [Property]
    public bool SplitPeriodsDifferByAtMostOneUnit(
        [MemberGen(nameof(Balances))] Money balance,
        [MemberGen(nameof(Rates))] InterestRates rates,
        [Between(0, 200)] int firstDays,
        [Between(0, 200)] int secondDays)
    {
        var whole = rates.Accrue(balance, firstDays + secondDays);
        var split = rates.Accrue(balance, firstDays) + rates.Accrue(balance, secondDays);
        return Math.Abs((whole - split).Amount) <= balance.Currency.Unit;
    }

    // Metamorphic: more days on the same positive balance never earn less.
    [Property]
    public bool MoreDaysNeverEarnLess(
        [MemberGen(nameof(Balances))] Money balance,
        [MemberGen(nameof(Rates))] InterestRates rates,
        [Between(0, 365)] int days)
    {
        Property.Precondition(!balance.IsNegative);
        return rates.Accrue(balance, days + 1) >= rates.Accrue(balance, days);
    }
}
