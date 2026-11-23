# Banking demo

This demo tests a small bank with GalaxyCheck and xunit v3. The domain has money in five currencies, a
double-entry ledger, transfers with overdraft limits, and daily interest. Money is a domain where one lost cent is a
real fault. Thus each property in this demo states a rule about money that must always hold.

## The domain

The project `Banking` holds the domain code:

- `Money.cs`: `Currency` and `Money`. A `Money` value has at most the decimal places of its currency.
  `Allocate` splits an amount in proportion to ratios, and the parts add up to the exact total. `ToString` and
  `Parse` use the format `USD -1,234.50`.
- `Ledger.cs`: a double-entry ledger. Each transaction has postings that add up to zero in each currency. The
  ledger keeps a running balance for each account, and the full journal for audit.
- `Bank.cs`: deposits, withdrawals, and transfers. A customer account can go below zero only down to its overdraft
  limit. Money comes in and goes out through one internal cash account for each currency.
- `Interest.cs`: simple daily interest. A positive balance earns the credit rate. A negative balance pays the
  overdraft rate.

## Run the tests

```
cd examples/Banking/Banking.Tests
dotnet test
```

## The properties

Each line gives the property and the fault that it finds.

`MoneyProperties.cs`:

- `FormatThenParseGivesTheSameMoney` (round trip): a format with the wrong count of decimal places, or a parser that
  drops the sign or the group separators.
- `RoundingTwiceIsTheSameAsRoundingOnce`: a rounding function that moves a value that is already rounded.
- `RoundingMovesAnAmountByAtMostHalfAUnit`: rounding to the wrong count of decimal places, or truncation in place of
  rounding.
- `AllocationAddsUpToTheTotal` (invariant): a split that creates or loses a minor unit.
- `AllocationAddsUpForTheSavedCounterexample` (replay): the return of the fault that the section "A true failure"
  shows.
- `EachPartIsWithinOneUnitOfItsExactShare`: leftover units that all go to one part.
- `AllocatingANegativeAmountMirrorsThePositiveAmount` (metamorphic): a split that rounds a debt in the wrong
  direction.
- `AllocationAgreesWithOneUnitAtATime` (oracle): a fast split that gives the leftover units to the wrong parts. The
  oracle hands out one cent at a time, slowly and obviously right.
- `YenAllocationUsesWholeYen` (with `[Example]` rows): a split that gives a fraction of a yen. The examples run
  first: 1 yen in three parts, and a zero ratio.
- `MoneyOfDifferentCurrenciesCannotBeAdded` (precondition): addition of dollars to euros.

`TransferProperties.cs`:

- `TransferWithinAvailableFundsMovesTheExactAmount` (precondition): an overdraft check that refuses a legal transfer,
  or a transfer that moves the wrong amount.
- `TransferAboveAvailableFundsFailsAndChangesNothing` (precondition): a refused transfer that still writes a
  transaction.
- `TransfersKeepTheCustomerTotal` (invariant): a transfer that creates or destroys money, or an account that goes
  past its overdraft limit.
- `RunningBalancesAgreeWithTheJournal` (invariant): a running balance that does not agree with the journal, or a
  ledger whose accounts do not add up to zero.
- `InterestHasTheSignOfTheBalance`: interest that pays a customer for a debt.
- `SplitPeriodsDifferByAtMostOneUnit` (metamorphic): a day count that changes with the length of the period.
- `MoreDaysNeverEarnLess` (metamorphic): interest that goes down when the time goes up.

`BankModelTests.cs`:

- `BankAgreesWithTheModel` (model-based): any difference between the bank and a dictionary of balances, after a
  random sequence of open, deposit, withdraw, and transfer commands.

## Patterns to copy

- **Generators with LINQ.** `BankingGen.cs` builds `Money` from a currency and a count of minor units with `from`,
  `from`, and `select`. The currency comes first, so the shrinker keeps the currency of a failure.
- **Member generators.** `[MemberGen(nameof(AnyMoney))]` gives a parameter its generator from a static property.
- **Preconditions.** `Property.Precondition(amountCents <= aliceCents + limitCents)` discards the cases that do not
  apply. Use a precondition only when most cases pass it. Otherwise, build a generator for the cases that you want.
- **A model.** `BankModel` is an immutable dictionary. Each command changes the model with `NextModel` and runs on
  the bank with `Run`. The postcondition compares the two. `Expects` lets a refused payment pass, and the
  postcondition then makes sure that the bank changed no balance.

## A true failure

To make this failure, we changed `Money.Allocate` in a scratch copy. The bug splits the signed amount, and not its
absolute value:

```csharp
long total = ToMinorUnits();   // was: Math.Abs(ToMinorUnits())
long sign = 1;                 // was: Amount < 0 ? -1 : 1
```

Integer division in C# rounds toward zero. Thus, for a debt, each part rounds up, and the leftover units become
negative. `dotnet test` gave this output for `AllocationAddsUpToTheTotal`:

```
Failed Banking.Tests.MoneyProperties.AllocationAddsUpToTheTotal [7 ms]
Error Message:
 GalaxyCheck.Runners.PropertyFailedException :
Falsified after 3 tests (6 shrinks)
Reproduction:
    [Replay("ArOXnsMFAgcAAQEBAQEA")]
Counterexample:
    [0] = { Amount = -0.01, Currency = { Code = "USD", MinorUnits = 2, Unit = 0.01 }, IsNegative = True, IsPositive = False }
    [1] = [1, 1]
Counterexample as C#:
    { Amount = -0.01, Currency = { Code = "USD", MinorUnits = 2, Unit = 0.01 }, IsNegative = True, IsPositive = False }
    new global::System.Collections.Generic.List<int> { 1, 1 }
Property function returned false
<galaxycheck-failure>
{"counterexample":["{ Amount = -0.01, Currency = { Code = \"USD\", MinorUnits = 2, Unit = 0.01 }, IsNegative = True, IsPositive = False }","[1, 1]"],"csharp":[null,"new global::System.Collections.Generic.List<int> { 1, 1 }"],"replay":"ArOXnsMFAgcAAQEBAQEA","seed":1483180979,"size":2,"iterations":3,"shrinks":6,"test":"Banking.Tests.MoneyProperties.AllocationAddsUpToTheTotal","rerun":"dotnet test --filter FullyQualifiedName=Banking.Tests.MoneyProperties.AllocationAddsUpToTheTotal"}
</galaxycheck-failure>
```

The shrunk counterexample is the smallest case of the fault: one cent of debt in two equal parts. The replay string
went into `AllocationAddsUpForTheSavedCounterexample` as `[Replay("ArOXnsMFAgcAAQEBAQEA")]`. That test runs only
this one case. With the bug, it fails with the same counterexample. Without the bug, it passes.

The same run also found the bug with the metamorphic property `AllocatingANegativeAmountMirrorsThePositiveAmount`.

A second bug in a scratch copy let `RequireFunds` ignore the overdraft limit. The model-based test then gave the
shortest command sequence that shows the fault:

```
Failed Banking.Tests.BankModelTests.BankAgreesWithTheModel [198 ms]
Error Message:
 GalaxyCheck.Runners.PropertyFailedException :
Falsified after 38 tests (3 shrinks)
Reproduction:
    [Replay("Ar/AyoUHJQcBAAEBAgAA")]
Counterexample: [Open alice with limit 50, Withdraw USD 0.01 from alice]
Counterexample as C#:
    [Open alice with limit 50, Withdraw USD 0.01 from alice]
---- The postcondition of command 2 failed.
Commands:
1. Open alice with limit 50
2. Withdraw USD 0.01 from alice <- failed
```

## Mutations that the properties find

Each bug went into a scratch copy only. No bug is in the code of this folder.

| Bug in the domain code | Properties that failed |
| --- | --- |
| `Allocate` splits the signed amount, not its absolute value | `AllocationAddsUpToTheTotal`, `AllocatingANegativeAmountMirrorsThePositiveAmount`, `AllocationAddsUpForTheSavedCounterexample` |
| `RequireFunds` ignores the overdraft limit | `BankAgreesWithTheModel`, `TransferWithinAvailableFundsMovesTheExactAmount`, `RunningBalancesAgreeWithTheJournal` |
| `ToString` always writes two decimal places | `FormatThenParseGivesTheSameMoney` (counterexample: BHD 0.001) |

## Words used here

- **Minor unit**: the smallest unit of a currency, for example one cent. JPY has no minor unit, and BHD has three
  decimal places.
- **Double entry**: each transaction takes money from one or more accounts and puts the same money into others.
- **Shrink**: the search for a smaller counterexample that still fails.
- **Invariant**: a rule that holds before and after each operation.
- **Precondition**: a condition that a test case must meet. GalaxyCheck discards a case that does not meet it.
- **Postcondition**: a condition that must hold after a command of a model-based test.
- **Immutable**: a value that does not change after its creation.
