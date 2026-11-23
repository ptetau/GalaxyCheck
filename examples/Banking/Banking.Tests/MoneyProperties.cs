using GalaxyCheck;
using GalaxyCheck.Gens.Injection.Int32;

namespace Banking.Tests;

public class MoneyProperties
{
    public static IGen<Money> AnyMoney => BankingGen.Money;

    public static IGen<IReadOnlyList<int>> Ratios => BankingGen.Ratios;

    // Round trip: the text of an amount parses back to the same amount and currency.
    [Property]
    public bool FormatThenParseGivesTheSameMoney([MemberGen(nameof(AnyMoney))] Money money) =>
        Money.Parse(money.ToString()) == money;

    [Property]
    public bool RoundingTwiceIsTheSameAsRoundingOnce(decimal amount, [MemberGen(nameof(Currencies))] Currency currency)
    {
        var once = Money.Round(amount, currency);
        return Money.Round(once.Amount, currency) == once;
    }

    public static IGen<Currency> Currencies => BankingGen.Currency;

    [Property]
    public bool RoundingMovesAnAmountByAtMostHalfAUnit(
        [MemberGen(nameof(Currencies))] Currency currency,
        [MemberGen(nameof(Fractions))] decimal amount) =>
        Math.Abs(Money.Round(amount, currency).Amount - amount) <= currency.Unit / 2;

    public static IGen<decimal> Fractions => Gen.Decimal().Between(-1_000_000m, 1_000_000m);

    // Invariant: allocation never creates or loses a minor unit.
    [Property]
    public bool AllocationAddsUpToTheTotal(
        [MemberGen(nameof(AnyMoney))] Money total,
        [MemberGen(nameof(Ratios))] IReadOnlyList<int> ratios)
    {
        var parts = total.Allocate(ratios);
        return parts.Aggregate(Money.Zero(total.Currency), (sum, part) => sum + part) == total;
    }

    // A replay of a saved counterexample: USD -0.01 in two equal parts once lost a cent. The replay runs only that
    // case, so the fault stays fixed even if a change to the generators moves the random cases.
    [Property]
    [Replay("ArOXnsMFAgcAAQEBAQEA")]
    public bool AllocationAddsUpForTheSavedCounterexample(
        [MemberGen(nameof(AnyMoney))] Money total,
        [MemberGen(nameof(Ratios))] IReadOnlyList<int> ratios) =>
        AllocationAddsUpToTheTotal(total, ratios);

    // Invariant: each part is within one minor unit of its exact share.
    [Property]
    public bool EachPartIsWithinOneUnitOfItsExactShare(
        [MemberGen(nameof(AnyMoney))] Money total,
        [MemberGen(nameof(Ratios))] IReadOnlyList<int> ratios)
    {
        var parts = total.Allocate(ratios);
        var sum = ratios.Sum();
        return parts.Select((part, i) => Math.Abs(part.Amount - total.Amount * ratios[i] / sum))
            .All(error => error < total.Currency.Unit);
    }

    // Metamorphic: to split a debt is to split the credit and negate each part.
    [Property]
    public bool AllocatingANegativeAmountMirrorsThePositiveAmount(
        [MemberGen(nameof(AnyMoney))] Money total,
        [MemberGen(nameof(Ratios))] IReadOnlyList<int> ratios) =>
        (-total).Allocate(ratios).SequenceEqual(total.Allocate(ratios).Select(part => -part));

    // Oracle: the fast allocation agrees with a slow version that hands out one minor unit at a time.
    [Property]
    public bool AllocationAgreesWithOneUnitAtATime(
        [Between(0, 5_000)] int totalCents,
        [MemberGen(nameof(Ratios))] IReadOnlyList<int> ratios)
    {
        var fast = Money.FromMinorUnits(totalCents, Currency.Usd).Allocate(ratios).Select(m => m.ToMinorUnits());
        return fast.SequenceEqual(SlowAllocation.OneUnitAtATime(totalCents, ratios));
    }

    // Known edge cases run first: a currency with no minor units, one yen in three parts, and a zero ratio.
    [Property]
    [Example(1, 1, 1, 1)]
    [Example(100, 1, 1, 1)]
    [Example(10, 0, 1, 0)]
    public bool YenAllocationUsesWholeYen(
        [Between(0, 100_000)] int yen,
        [Between(0, 9)] int first,
        [Between(0, 9)] int second,
        [Between(1, 9)] int third)
    {
        var parts = Money.FromMinorUnits(yen, Currency.Jpy).Allocate([first, second, third]);
        return parts.All(part => part.Amount == decimal.Truncate(part.Amount)) && parts.Sum(p => p.Amount) == yen;
    }

    [Property]
    public void MoneyOfDifferentCurrenciesCannotBeAdded(
        [MemberGen(nameof(AnyMoney))] Money left,
        [MemberGen(nameof(AnyMoney))] Money right)
    {
        Property.Precondition(left.Currency != right.Currency);
        Assert.Throws<CurrencyMismatchException>(() => left + right);
    }
}

/// <summary>The oracle of allocation: slow and simple, and thus easy to trust.</summary>
internal static class SlowAllocation
{
    /// <summary>Gives each minor unit, one at a time, to the part that is furthest below its exact share.</summary>
    public static long[] OneUnitAtATime(long total, IReadOnlyList<int> ratios)
    {
        long sum = ratios.Sum();
        var shares = new long[ratios.Count];
        for (long unit = 0; unit < total; unit++)
        {
            // The shortfall of part i, times the sum of the ratios, is an exact integer.
            var best = Enumerable.Range(0, ratios.Count).MaxBy(i => total * ratios[i] - shares[i] * sum);
            shares[best]++;
        }

        return shares;
    }
}
