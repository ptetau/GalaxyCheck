namespace Banking;

public sealed class InsufficientFundsException(string accountId, Money requested, Money available)
    : InvalidOperationException($"Account '{accountId}' cannot pay {requested}: only {available} is available.");

/// <summary>
/// Customer operations on top of a ledger. Money comes in and goes out through one internal cash account for each
/// currency, and interest comes from one internal interest account for each currency. A customer account can go
/// below zero only down to its overdraft limit.
/// </summary>
public sealed class Bank
{
    public Ledger Ledger { get; } = new();

    public void Open(string accountId, Currency currency, decimal overdraftLimit = 0m)
    {
        if (overdraftLimit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(overdraftLimit), "An overdraft limit cannot be negative.");
        }

        if (accountId.StartsWith('#'))
        {
            throw new ArgumentException("Customer account IDs cannot start with '#'.", nameof(accountId));
        }

        EnsureInternal(currency);
        Ledger.Open(new Account(accountId, currency, Money.Of(overdraftLimit, currency), IsInternal: false));
    }

    public Money Balance(string accountId) => Ledger.Balance(accountId);

    /// <summary>The money that a customer account can still pay out: its balance plus its overdraft limit.</summary>
    public Money Available(string accountId) => Ledger.Balance(accountId) + Ledger.Get(accountId).OverdraftLimit;

    public void Deposit(string accountId, Money amount)
    {
        RequirePositive(amount);
        Ledger.Post("deposit", new Posting(CashId(amount.Currency), -amount), new Posting(accountId, amount));
    }

    public void Withdraw(string accountId, Money amount)
    {
        RequirePositive(amount);
        RequireFunds(accountId, amount);
        Ledger.Post("withdrawal", new Posting(accountId, -amount), new Posting(CashId(amount.Currency), amount));
    }

    public void Transfer(string fromId, string toId, Money amount)
    {
        if (fromId == toId)
        {
            throw new ArgumentException("A transfer needs two different accounts.");
        }

        RequirePositive(amount);
        var to = Ledger.Get(toId);
        if (to.Currency != amount.Currency)
        {
            throw new CurrencyMismatchException(to.Currency, amount.Currency);
        }

        RequireFunds(fromId, amount);
        Ledger.Post($"transfer to {toId}", new Posting(fromId, -amount), new Posting(toId, amount));
    }

    /// <summary>Posts the interest of <paramref name="days"/> days on the current balance, at the rates of <paramref name="rates"/>.</summary>
    public Money ApplyInterest(string accountId, InterestRates rates, int days)
    {
        var interest = rates.Accrue(Balance(accountId), days);
        if (interest.Amount != 0m)
        {
            Ledger.Post("interest", new Posting(InterestId(interest.Currency), -interest), new Posting(accountId, interest));
        }

        return interest;
    }

    /// <summary>The sum of the balances of all customer accounts in one currency.</summary>
    public Money CustomerTotal(Currency currency) => Ledger.Accounts
        .Where(a => !a.IsInternal && a.Currency == currency)
        .Aggregate(Money.Zero(currency), (sum, a) => sum + Ledger.Balance(a.Id));

    private void RequireFunds(string accountId, Money amount)
    {
        var account = Ledger.Get(accountId);
        if (account.Currency != amount.Currency)
        {
            throw new CurrencyMismatchException(account.Currency, amount.Currency);
        }

        var available = Available(accountId);
        if (amount > available)
        {
            throw new InsufficientFundsException(accountId, amount, available);
        }
    }

    private static void RequirePositive(Money amount)
    {
        if (!amount.IsPositive)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), $"The amount must be above zero, not {amount}.");
        }
    }

    private void EnsureInternal(Currency currency)
    {
        foreach (var id in new[] { CashId(currency), InterestId(currency) })
        {
            if (!Ledger.Exists(id))
            {
                Ledger.Open(new Account(id, currency, Money.Zero(currency), IsInternal: true));
            }
        }
    }

    private static string CashId(Currency currency) => "#cash-" + currency.Code;

    private static string InterestId(Currency currency) => "#interest-" + currency.Code;
}
