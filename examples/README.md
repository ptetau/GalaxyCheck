# GalaxyCheck examples

This folder holds three small demos of property-based testing with GalaxyCheck. Each demo has real domain code and
a suite of properties that find real faults in that kind of code. Each demo uses a different test framework.

| Demo | Domain | Test framework | Properties |
| --- | --- | --- | --- |
| [Banking](Banking/README.md) | Money, a double-entry ledger, transfers, overdrafts, interest | xunit v3 (`GalaxyCheck.Xunit.V3`) | 18 |
| [Physics](Physics/README.md) | Vectors, units, kinematics, collisions, a spring, an orbit | TUnit (`GalaxyCheck.TUnit`) | 24 |
| [CRM](Crm/README.md) | Contacts, normalisation, deduplication, companies, lead scores, a pipeline, search | NUnit (`GalaxyCheck.NUnit`) | 33 |

A property is a rule that must hold for each input. GalaxyCheck generates many inputs and runs the rule on each.
When the rule fails, GalaxyCheck shrinks the input to a small counterexample, and gives a replay string that runs
that case again.

## Build and run

Each project targets net10.0 and refers to the GalaxyCheck projects in `src/`. Thus the demos always test the
current code of the library.

```
dotnet build examples/Examples.slnx

(cd examples/Banking/Banking.Tests && dotnet test)
(cd examples/Physics && dotnet test --project Physics.Tests)
(cd examples/Crm/Crm.Tests && dotnet test)
```

The Physics demo has its own `global.json`, because TUnit needs Microsoft.Testing.Platform. Run it from the folder
`examples/Physics`.

A failed property saves its replay in a folder `.galaxycheck` next to the test project, and the next run tries it
first. The file `.gitignore` keeps these folders, `bin`, `obj`, and `TestResults` out of Git.

## Where to find each pattern

| Pattern | Banking | Physics | CRM |
| --- | --- | --- | --- |
| Custom generator with LINQ (`from`, `select`) | `BankingGen.Money` | `PhysicsGen.AnyScaleVector`, `TouchingPair` | `CrmGen.ContactsWithDuplicates`, `Tag` |
| `[MemberGen]` | most properties | most properties | most properties |
| `[Example]` rows for known edge cases | `YenAllocationUsesWholeYen` | `FahrenheitToCelsiusFollowsTheDefinition` | `InvalidAddressesAreRejected`, `OneNorthAmericanNumberHasOneKey` |
| `[Replay]` of a saved counterexample | `AllocationAddsUpForTheSavedCounterexample` | | |
| Exhaustive run of a small domain | | | `AnOpenDealNeverSkipsAStage`, `TheStageRulesAreCheckedExhaustively` |
| Model-based (stateful) test | `BankAgreesWithTheModel` | | |
| Round trip | `FormatThenParseGivesTheSameMoney` | `LengthConversionRoundTrips`, `TemperatureConversionRoundTrips` | |
| Invariant | `TransfersKeepTheCustomerTotal`, `AllocationAddsUpToTheTotal` | `CrossProductIsPerpendicularToBothInputs` | `NoTwoContactsShareAKeyAfterDeduplication` |
| Metamorphic | `SplitPeriodsDifferByAtMostOneUnit` | `CollisionLooksTheSameFromAMovingFrame` | `CaseSpacesAndSubAddressDoNotMatter`, `DeduplicationDoesNotDependOnTheOrder` |
| Oracle (compare with a simple, slow version) | `AllocationAgreesWithOneUnitAtATime` | `SpringFollowsTheExactSolution` (exact solution) | `DeduplicationAgreesWithPairwiseMerging`, `SearchAgreesWithALinearScan` |
| `Property.Precondition` | `TransferWithinAvailableFundsMovesTheExactAmount` | `BodiesThatMoveApartKeepTheirVelocities` | `MergeKeepsTheOldestIdAndTheNewestName` |
| `Cover` and `Label` | | `NormalizedVectorHasLengthOne`, `TimeToReachIsTheInverseOfPosition` | `NoTwoContactsShareAKeyAfterDeduplication`, `GroupEmployeesAreTheSumOverTheWholeTree` |
| Floating-point edge cases (NaN, infinity, subnormal) | | `SpecialComponentsGiveTheExpectedLength`, `SpecialValuesPassThroughConversion` | |
| `Gen.Create<T>()` with `OverrideMember` | | | `CrmGen.ContactWith` |
| `Gen.Realistic` and `Gen.Email` | | | `CrmGen.ContactWith` |
| `Gen.StringMatching` (pattern strings) | | | `CrmGen.InternationalPhone`, `NationalNumbersGetTheCountryCodeAndLoseTheTrunkZero` |
| `Gen.Recursive` (nested data) | | | `CrmGen.CompanyGroup` |
| Merge laws (commutative, associative, idempotent, nothing lost) | | | `MergeProperties` |

## How each demo proves that its properties can fail

A property that cannot fail proves nothing. For each demo, we put plausible bugs into a scratch copy of the domain
code, ran the tests, and saw the properties fail. Each README lists these mutations, and shows the true output of
one failure. No bug is in the code of this folder.
