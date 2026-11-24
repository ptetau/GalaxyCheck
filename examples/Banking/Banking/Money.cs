using System.Globalization;

namespace Banking;

/// <summary>A currency and the count of digits after its decimal point (its minor units).</summary>
public sealed record Currency(string Code, int MinorUnits)
{
    public static readonly Currency Usd = new("USD", 2);
    public static readonly Currency Eur = new("EUR", 2);
    public static readonly Currency Nzd = new("NZD", 2);
    public static readonly Currency Jpy = new("JPY", 0);
    public static readonly Currency Bhd = new("BHD", 3);

    public static IReadOnlyList<Currency> All { get; } = [Usd, Eur, Nzd, Jpy, Bhd];

    /// <summary>The size of one minor unit, for example 0.01 for USD and 1 for JPY.</summary>
    public decimal Unit => MinorUnits switch { 0 => 1m, 1 => 0.1m, 2 => 0.01m, 3 => 0.001m, _ => throw new InvalidOperationException() };

    public static Currency FromCode(string code) =>
        All.FirstOrDefault(c => c.Code == code) ?? throw new FormatException($"Unknown currency '{code}'.");

    public override string ToString() => Code;
}

public sealed class CurrencyMismatchException(Currency left, Currency right)
    : InvalidOperationException($"Cannot combine {left} with {right}.");

/// <summary>An amount of one currency. The amount always has at most the minor units of its currency.</summary>
public readonly record struct Money
{
    private Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public Currency Currency { get; }

    public static Money Zero(Currency currency) => new(0m, currency);

    /// <summary>Creates money from an exact amount. Throws when the amount has more digits than the currency allows.</summary>
    public static Money Of(decimal amount, Currency currency)
    {
        if (decimal.Round(amount, currency.MinorUnits) != amount)
        {
            throw new ArgumentException($"{amount} has more than {currency.MinorUnits} decimal places for {currency}.", nameof(amount));
        }

        return new Money(amount, currency);
    }

    public static Money FromMinorUnits(long minorUnits, Currency currency) => new(minorUnits * currency.Unit, currency);

    /// <summary>Rounds an amount to the minor unit with banker's rounding (a midpoint goes to the even digit).</summary>
    public static Money Round(decimal amount, Currency currency) =>
        new(decimal.Round(amount, currency.MinorUnits, MidpointRounding.ToEven), currency);

    public long ToMinorUnits() => decimal.ToInt64(Amount / Currency.Unit);

    public bool IsNegative => Amount < 0;

    public bool IsPositive => Amount > 0;

    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount, SameCurrency(left, right));

    public static Money operator -(Money left, Money right) => new(left.Amount - right.Amount, SameCurrency(left, right));

    public static Money operator -(Money value) => new(-value.Amount, value.Currency);

    public static bool operator <(Money left, Money right) => left.Amount < SameAmount(left, right);

    public static bool operator >(Money left, Money right) => left.Amount > SameAmount(left, right);

    public static bool operator <=(Money left, Money right) => left.Amount <= SameAmount(left, right);

    public static bool operator >=(Money left, Money right) => left.Amount >= SameAmount(left, right);

    /// <summary>
    /// Splits the money into parts in proportion to the ratios. The parts add up to the exact total. The leftover
    /// minor units go to the parts with the largest remainders; a tie goes to the part with the lower index.
    /// </summary>
    public IReadOnlyList<Money> Allocate(IReadOnlyList<int> ratios)
    {
        ValidateRatios(ratios);
        long sum = ratios.Sum(r => (long)r);
        long total = Math.Abs(ToMinorUnits());
        long sign = Amount < 0 ? -1 : 1;

        var shares = new long[ratios.Count];
        var remainders = new long[ratios.Count];
        long given = 0;
        for (var i = 0; i < ratios.Count; i++)
        {
            var exact = checked(total * ratios[i]);
            shares[i] = exact / sum;
            remainders[i] = exact % sum;
            given += shares[i];
        }

        var order = Enumerable.Range(0, ratios.Count).OrderByDescending(i => remainders[i]).ThenBy(i => i);
        foreach (var i in order.Take((int)(total - given)))
        {
            shares[i]++;
        }

        var currency = Currency;
        return shares.Select(s => FromMinorUnits(sign * s, currency)).ToList();
    }

    internal static void ValidateRatios(IReadOnlyList<int> ratios)
    {
        if (ratios.Count == 0 || ratios.Any(r => r < 0) || ratios.All(r => r == 0))
        {
            throw new ArgumentException("Give at least one ratio. Each ratio must be 0 or more, and one ratio must be above 0.", nameof(ratios));
        }
    }

    /// <summary>Formats as "USD -1,234.50": the code, a space, and the amount with group separators and each minor digit.</summary>
    public override string ToString() =>
        $"{Currency.Code} {Amount.ToString("N" + Currency.MinorUnits, CultureInfo.InvariantCulture)}";

    /// <summary>Parses the format of <see cref="ToString"/>. Group separators are optional.</summary>
    public static Money Parse(string text)
    {
        var parts = text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            throw new FormatException($"Expected '<code> <amount>', got '{text}'.");
        }

        var currency = Currency.FromCode(parts[0]);
        const NumberStyles styles = NumberStyles.AllowLeadingSign | NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint;
        if (!decimal.TryParse(parts[1], styles, CultureInfo.InvariantCulture, out var amount))
        {
            throw new FormatException($"'{parts[1]}' is not an amount.");
        }

        return Of(amount, currency);
    }

    private static Currency SameCurrency(Money left, Money right) =>
        left.Currency == right.Currency ? left.Currency : throw new CurrencyMismatchException(left.Currency, right.Currency);

    private static decimal SameAmount(Money left, Money right)
    {
        SameCurrency(left, right);
        return right.Amount;
    }
}
