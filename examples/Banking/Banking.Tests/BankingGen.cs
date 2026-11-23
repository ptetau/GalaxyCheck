using GalaxyCheck;

namespace Banking.Tests;

/// <summary>Generators of the banking domain, built from the generators of GalaxyCheck with LINQ.</summary>
public static class BankingGen
{
    public static IGen<Currency> Currency => Gen.Element(Banking.Currency.All);

    /// <summary>Money of one currency, from -100,000 to 100,000 major units.</summary>
    public static IGen<Money> MoneyIn(Currency currency) =>
        from minor in Gen.Int64().Between(-100_000 * Pow10(currency.MinorUnits), 100_000 * Pow10(currency.MinorUnits))
        select Banking.Money.FromMinorUnits(minor, currency);

    /// <summary>Money of any currency. The currency comes first, so that shrinking keeps a failing currency.</summary>
    public static IGen<Money> Money =>
        from currency in Currency
        from money in MoneyIn(currency)
        select money;

    /// <summary>Positive USD amounts from 0.01 to 500.00.</summary>
    public static IGen<Money> UsdAmount =>
        Gen.Int64().Between(1, 50_000).Select(cents => Banking.Money.FromMinorUnits(cents, Banking.Currency.Usd));

    /// <summary>From 1 to 8 ratios of 0 to 100, at least one above 0.</summary>
    public static IGen<IReadOnlyList<int>> Ratios =>
        Gen.Int32().Between(0, 100).ListOf().WithCountBetween(1, 8).Where(ratios => ratios.Any(r => r > 0));

    private static long Pow10(int exponent) => (long)Math.Pow(10, exponent);
}
