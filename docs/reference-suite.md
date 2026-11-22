# Reference suite

The reference suite holds the library to the laws and the best known shrinks that other property-based testing libraries use on themselves. Each case is written new from the idea. No test code is copied. This file names the source of the idea of each requirement, from R68 to R77.

## R68: Generator laws

The laws: the same seed gives the same values, shrinking ends, no accepted shrink repeats, the shrunk value still fails, and a run from the replay of the shrunk value makes no further shrink.

- fast-check, `ArbitraryAssertions`: https://github.com/dubzzz/fast-check
- jqwik, `ShrinkableProperties`: https://github.com/jqwik-team/jqwik
- QuickCheck, the `prop_no_shrinking_loop` tests: https://github.com/nick8325/quickcheck

## R69: Shrinking challenge

The challenges reverse, lengthlist, nestedlists, deletion, distinct, coupling, difference, large union list, bound5, binheap, and calculator, with their best known minimums.

- The shrinking challenge: https://github.com/jlink/shrinking-challenge

## R70: Shrink budget

A first failing value that is already the minimum shrinks with at most 50 more calls of the test function.

- Hypothesis, `test_shrink_budgeting`: https://github.com/HypothesisWorks/hypothesis

## R71: Distribution

A chi-squared check of uniformity, and the minimum and the maximum of each integer generator.

- CsCheck, the chi-squared tests: https://github.com/AnthonyLloyd/CsCheck
- Hypothesis, `test_discovery_ability`: https://github.com/HypothesisWorks/hypothesis

## R72: Replay laws

A replay runs one test case and makes no shrink. A replay from another generator, or a replay that is cut short or changed, gives an error.

- Hypothesis, `test_reproduce_failure` and `test_replay_logic`: https://github.com/HypothesisWorks/hypothesis
- fast-check, `ReplayFailures`: https://github.com/dubzzz/fast-check
- Hedgehog, `Skip`: https://github.com/hedgehogqa/haskell-hedgehog

## R73: Operator laws

`Select` keeps the functor laws. `SelectMany` keeps the monad laws.

- FsCheck, `Gen.fs`: https://github.com/fscheck/FsCheck
- Hedgehog, `hedgehog-test-laws`: https://github.com/hedgehogqa/haskell-hedgehog/tree/master/hedgehog-test-laws

## R74: Random programs

Random compositions of the generators and operators never crash or hang, and each failure shrinks and replays.

- Hypothesis, `test_explore_arbitrary_languages`: https://github.com/HypothesisWorks/hypothesis

## R75: Runner equivalence

A property and its async form give the same result. Two runs in one process or in two processes give the same result.

- FsCheck, `Runner.fs` and `Property.fs`: https://github.com/fscheck/FsCheck

## R76: Exhaustive laws

An exhaustive run gives each value of the domain one time. A domain that is too large runs the normal random test cases.

- jqwik, `ExhaustiveGenerationTests`: https://github.com/jqwik-team/jqwik
- fast-check, `GenerateAllValues`: https://github.com/dubzzz/fast-check

## R77: Bug-finding table

Eight binary search trees, each with one planted bug, and the five kinds of properties: validity, postcondition, metamorphic, inductive, and model-based.

- "How to Specify It", John Hughes: https://github.com/rjmh/how-to-specify-it
