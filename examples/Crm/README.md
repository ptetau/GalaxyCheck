# CRM demo

This demo tests the core of a customer relationship manager (CRM) with GalaxyCheck and NUnit. The domain has
contacts, email and phone normalisation, deduplication and merge, company groups, lead scores, a sales pipeline,
and contact search. CRM data is messy: people type the same address in upper case, with a sub-address, or with
spaces. Thus the generators here make realistic and messy data.

## The domain

The project `Crm` holds the domain code:

- `Normalisation.cs`: `EmailAddress.Normalise` and `PhoneNumber.Normalise`. Each returns a canonical key, or null
  for text that is not an address or a number.
- `Contacts.cs`: `Contact`, `ContactMerge.Merge`, and `Deduplicator.Deduplicate`. Two contacts are one person if
  they share a normalised email address or phone number. Sharing is transitive.
- `Sales.cs`: `Company` (a tree of subsidiaries), `LeadScore`, and the `Pipeline` rules of a `Deal`.
- `Search.cs`: `ContactIndex`, a sorted index of words with a binary search for each prefix.

## Run the tests

```
cd examples/Crm/Crm.Tests
dotnet test
```

## The properties

Each line gives the property and the fault that it finds.

`NormalisationProperties.cs`:

- `EveryGeneratedEmailIsValid`: a validator that refuses real addresses, for example with a dot or an underscore.
- `EmailNormalisationIsIdempotent`: a normaliser whose output it does not accept again.
- `CaseSpacesAndSubAddressDoNotMatter` (metamorphic): a key that changes with the case, white space at the ends, or a
  sub-address.
- `InvalidAddressesAreRejected` (with `[Example]` rows): a validator that accepts "ann@", "ann@@example.com", or
  "ann@example". The examples run first, and then text with no "@".
- `PhoneNormalisationIsIdempotent`: a normaliser that adds the country code a second time.
- `SeparatorsDoNotMatter` (metamorphic): a key that changes with a space, dash, dot, or parenthesis between digits.
- `PlusAndDoubleZeroAreTheSamePrefix` (metamorphic): "+44" and "0044" that give two keys.
- `NationalNumbersGetTheCountryCodeAndLoseTheTrunkZero` (pattern strings): a national number that keeps its 0.
- `OneNorthAmericanNumberHasOneKey` (with `[Example]` rows): five spellings of one number that give more than one
  key.

`MergeProperties.cs`:

- `MergeIsCommutative`: a merge whose result depends on which record comes first.
- `MergeIsAssociative`: a merge whose result depends on the order of a chain of merges.
- `MergingAgainChangesNothing` (idempotent): a merge that adds a duplicate address or tag each time that it runs.
- `MergeLosesNoAddressNumberOrTag` (nothing lost): a merge that keeps only the values of one record.
- `MergeKeepsTheOldestIdAndTheNewestName` (precondition): a merge that keeps the wrong ID or the wrong name.
- `NoTwoContactsShareAKeyAfterDeduplication` (invariant, with cover): a deduplication that is not transitive.
- `DeduplicationKeepsEveryAddressAndNumber` (nothing lost): a deduplication that drops data.
- `DeduplicationIsIdempotent`: a deduplication that finds new duplicates in its own output.
- `DeduplicationDoesNotDependOnTheOrder` (metamorphic): a result that changes when the input is shuffled.
- `DeduplicationAgreesWithPairwiseMerging` (oracle): a union-find error. The oracle merges any two contacts that
  share a key, until no two do.

`SalesProperties.cs`:

- `GroupEmployeesAreTheSumOverTheWholeTree` (recursive data, with cover): a count that stops at the first level of
  subsidiaries.
- `AGroupIsNeverInASmallerSegmentThanItsMembers`: the same fault, seen as a segment.
- `FlattenListsEachCompanyOneTime`: a walk of the tree that skips or repeats a company.
- `ScoreIsFrom0To100`: a score with no clamp.
- `AnotherActivityNeverLowersTheScore` (metamorphic): a rule that punishes activity.
- `FutureActivitiesDoNotCount` (metamorphic): a score that reads the future.
- `AnOpenDealNeverSkipsAStage` (exhaustive): a pipeline rule that lets a deal skip a stage.
- `AWonDealIsFinal` (exhaustive): a won deal that can move again.
- `OnlyNegotiationLeadsToWon` (exhaustive): a deal that is won from the wrong stage.
- `EachStageExceptWonCanStillReachWon` (exhaustive): a stage with no path to a sale.
- `TheStageRulesAreCheckedExhaustively`: a plain NUnit test that shows `result.Exhaustive` for the 36 pairs of
  stages.
- `DealHistoryHoldsOnlyAllowedMoves` (invariant): a deal that changes its stage after a refused move.
- `SearchAgreesWithALinearScan` (oracle, with cover): an index that misses or adds a contact.
- `EachContactIsFoundByItsFullName`: an off-by-one error in the binary search.
- `AnotherWordNeverAddsResults` (metamorphic): a search that joins the words of a query with "or".

## Patterns to copy

- **Automatic generation with overrides.** `Gen.Create<Contact>()` builds each member from its type.
  `.OverrideMember(c => c.FirstName, Gen.Realistic.FirstName())` replaces one member. `CrmGen.ContactWith` overrides
  each member where a random string is a poor value.
- **Realistic data.** `Gen.Realistic.FirstName()`, `LastName()`, `CompanyName()`, `PhoneNumber()`, and `Gen.Email()`
  give values that look real.
- **Pattern strings.** `Gen.StringMatching(@"(\+|00)(44|49|61|64) ?[1-9][0-9]{2} ?[0-9]{3} ?[0-9]{3}")` gives
  phone numbers in the formats that people write.
- **Duplicates on purpose.** `ContactsWithDuplicates` draws addresses from a pool of four, so that most lists have
  duplicates. The cover rule of `NoTwoContactsShareAKeyAfterDeduplication` proves it.
- **Recursive data.** `Gen.Recursive(leaf, wrap, maxDepth: 3)` builds company trees. The leaf is a company with no
  subsidiaries, and `wrap` adds a level.
- **Exhaustive runs.** A property over two `Stage` values has 36 cases. That is fewer than the 100 iterations of a
  run, so GalaxyCheck tries each case one time.

## What the first runs found

The first runs of these properties found two real faults in the domain code and the generators:

- `MergeIsAssociative` failed. `Merge` broke a tie of activity dates by the lower ID. But a merged record takes its
  ID from the older record, and its name from the more recent one. Thus `(a + b) + c` and `a + (b + c)` compared
  different IDs. The tie now goes to the name that sorts first, which is a field that comes from the same record.
- `DeduplicationDoesNotDependOnTheOrder` failed for two contacts with one ID. The shrinker moves each Guid toward
  the smallest Guid, so it made the IDs equal. A real CRM has unique IDs, so `CrmGen.UniqueIds` now keeps only lists
  with unique IDs.

## A true failure

To make this failure, we changed `ContactMerge.Merge` in a scratch copy. The bug takes the name from the first
argument:

```csharp
var recent = a;   // was: Recent(a, b) ? a : b
```

`dotnet test` gave this output:

```
Failed MergeIsCommutative [10 ms]
Error Message:
 GalaxyCheck.Runners.PropertyFailedException :

Falsified after 1 test (7 shrinks)
Reproduction:
    [Replay("AtmhgIICABK/6b2lpI7u580B2Y2c6rengcAcAAAAAAAAAICK/Pb57Lf1L828h+/NwOfzEAABAAAAAAA=")]
Counterexample:
    [0] = { Id = 44af74bf-b872-cdcf-d906-477d3b05801c, FirstName = "James", LastName = "Smith", Emails = [], Phones = [], Tags = [], CreatedOn = 01/01/2020, LastActivityOn = 01/01/2020, FullName = "James Smith", EmailKeys = [], ... }
    [1] = { Id = 9edf0500-df67-2fea-4dde-e1dd049ee710, FirstName = "James", LastName = "Johnson", Emails = [], Phones = [], Tags = [], CreatedOn = 01/01/2020, LastActivityOn = 01/01/2020, FullName = "James Johnson", EmailKeys = [], ... }
Counterexample as C#:
    { Id = 44af74bf-b872-cdcf-d906-477d3b05801c, FirstName = "James", LastName = "Smith", Emails = [], Phones = [], Tags = [], CreatedOn = 01/01/2020, LastActivityOn = 01/01/2020, FullName = "James Smith", EmailKeys = [], ... }
    { Id = 9edf0500-df67-2fea-4dde-e1dd049ee710, FirstName = "James", LastName = "Johnson", Emails = [], Phones = [], Tags = [], CreatedOn = 01/01/2020, LastActivityOn = 01/01/2020, FullName = "James Johnson", EmailKeys = [], ... }

Property function returned false

<galaxycheck-failure>
{"counterexample":["{ Id = 44af74bf-b872-cdcf-d906-477d3b05801c, FirstName = \"James\", LastName = \"Smith\", Emails = [], Phones = [], Tags = [], CreatedOn = 01/01/2020, LastActivityOn = 01/01/2020, FullName = \"James Smith\", EmailKeys = [], ... }","{ Id = 9edf0500-df67-2fea-4dde-e1dd049ee710, FirstName = \"James\", LastName = \"Johnson\", Emails = [], Phones = [], Tags = [], CreatedOn = 01/01/2020, LastActivityOn = 01/01/2020, FullName = \"James Johnson\", EmailKeys = [], ... }"],"csharp":[null,null],"replay":"AtmhgIICABK/6b2lpI7u580B2Y2c6rengcAcAAAAAAAAAICK/Pb57Lf1L828h+/NwOfzEAABAAAAAAA=","seed":541069529,"size":0,"iterations":1,"shrinks":7,"test":"Crm.Tests.MergeProperties.MergeIsCommutative","rerun":"dotnet test --filter FullyQualifiedName=Crm.Tests.MergeProperties.MergeIsCommutative"}
</galaxycheck-failure>
```

The shrunk contacts have no addresses, no numbers, no tags, and the same dates. Only the names differ, and the name
is the fault.

## Mutations that the properties find

Each bug went into a scratch copy only. No bug is in the code of this folder.

| Bug in the domain code | Properties that failed |
| --- | --- |
| `Merge` takes the name from the first argument | `MergeIsCommutative`, `MergeKeepsTheOldestIdAndTheNewestName`, `DeduplicationDoesNotDependOnTheOrder` |
| `Deduplicate` links a contact, not its group (`parent[i] = other`) | `NoTwoContactsShareAKeyAfterDeduplication`, `DeduplicationIsIdempotent`, `DeduplicationDoesNotDependOnTheOrder`, `DeduplicationAgreesWithPairwiseMerging` |
| The binary search of `ContactIndex` uses `<=` in place of `<` | `EachContactIsFoundByItsFullName` |
| `GroupEmployees` counts only the first level of subsidiaries | `GroupEmployeesAreTheSumOverTheWholeTree`, `AGroupIsNeverInASmallerSegmentThanItsMembers` |

## Words used here

- **Normalise**: change a value to one canonical form, so that two spellings of one value become equal.
- **Sub-address**: the part of an email address after a "+", for example "news" in "ann+news@example.com".
- **Trunk prefix**: the 0 at the start of a national phone number, which an international number drops.
- **Union-find**: a data structure that joins items into groups and finds the group of an item.
- **Idempotent**: an operation that gives the same result when it runs a second time on its own output.
- **Invariant**: a rule that holds for each output, for example that no two contacts share an address.
- **Precondition**: a condition that a test case must meet. GalaxyCheck discards a case that does not meet it.
