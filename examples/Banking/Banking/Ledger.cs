namespace Banking;

/// <summary>One line of a transaction: a signed amount for one account. A positive amount adds to the balance.</summary>
public sealed record Posting(string AccountId, Money Amount);

/// <summary>A balanced set of postings. The postings of each currency add up to zero.</summary>
public sealed record Transaction(long Number, string Description, IReadOnlyList<Posting> Postings);

public sealed record Account(string Id, Currency Currency, Money OverdraftLimit, bool IsInternal);

public sealed class UnbalancedTransactionException(string message) : InvalidOperationException(message);

/// <summary>
/// A double-entry ledger. Each transaction moves money between accounts and never creates or destroys it.
/// The ledger keeps a running balance for each account, and the full journal for audit.
/// </summary>
public sealed class Ledger
{
    private readonly Dictionary<string, Account> _accounts = new();
    private readonly Dictionary<string, Money> _balances = new();
    private readonly List<Transaction> _journal = new();

    public IReadOnlyList<Transaction> Journal => _journal;

    public IEnumerable<Account> Accounts => _accounts.Values;

    public void Open(Account account)
    {
        if (!_accounts.TryAdd(account.Id, account))
        {
            throw new InvalidOperationException($"Account '{account.Id}' exists.");
        }

        _balances[account.Id] = Money.Zero(account.Currency);
    }

    public bool Exists(string accountId) => _accounts.ContainsKey(accountId);

    public Account Get(string accountId) =>
        _accounts.TryGetValue(accountId, out var account) ? account : throw new KeyNotFoundException($"No account '{accountId}'.");

    public Money Balance(string accountId) => _balances.TryGetValue(accountId, out var balance)
        ? balance
        : throw new KeyNotFoundException($"No account '{accountId}'.");

    /// <summary>The balance from the journal alone, without the running balance.</summary>
    public Money BalanceFromJournal(string accountId)
    {
        var account = Get(accountId);
        return _journal
            .SelectMany(t => t.Postings)
            .Where(p => p.AccountId == accountId)
            .Aggregate(Money.Zero(account.Currency), (sum, p) => sum + p.Amount);
    }

    public Transaction Post(string description, params Posting[] postings)
    {
        if (postings.Length < 2)
        {
            throw new UnbalancedTransactionException("A transaction needs at least two postings.");
        }

        foreach (var posting in postings)
        {
            var account = Get(posting.AccountId);
            if (posting.Amount.Currency != account.Currency)
            {
                throw new CurrencyMismatchException(account.Currency, posting.Amount.Currency);
            }
        }

        foreach (var group in postings.GroupBy(p => p.Amount.Currency))
        {
            var sum = group.Aggregate(Money.Zero(group.Key), (total, p) => total + p.Amount);
            if (sum.Amount != 0m)
            {
                throw new UnbalancedTransactionException($"The {group.Key} postings add up to {sum}, not zero.");
            }
        }

        var transaction = new Transaction(_journal.Count + 1, description, postings);
        _journal.Add(transaction);
        foreach (var posting in postings)
        {
            _balances[posting.AccountId] += posting.Amount;
        }

        return transaction;
    }
}
