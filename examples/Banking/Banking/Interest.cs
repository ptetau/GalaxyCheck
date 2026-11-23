namespace Banking;

/// <summary>
/// Simple daily interest with an actual/365 day count. A positive balance earns the credit rate. A negative
/// balance pays the overdraft rate, so its interest is negative. Rates are percentages per year.
/// </summary>
public sealed record InterestRates(decimal CreditPercent, decimal OverdraftPercent)
{
    public Money Accrue(Money balance, int days)
    {
        if (days < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(days), "Days cannot be negative.");
        }

        var percent = balance.IsNegative ? OverdraftPercent : CreditPercent;
        var exact = balance.Amount * percent / 100m * days / 365m;
        return Money.Round(exact, balance.Currency);
    }
}
