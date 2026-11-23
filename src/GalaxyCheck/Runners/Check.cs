using GalaxyCheck.Gens.Internal;
using GalaxyCheck.Gens.Parameters;
using GalaxyCheck.Properties;
using GalaxyCheck.Runners;
using GalaxyCheck.Runners.Check;
using GalaxyCheck.Runners.Replaying;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GalaxyCheck
{
    public static partial class Extensions
    {
        /// <summary>Runs the property and returns the result, with the shrunk counterexample of a failure.</summary>
        public static CheckResult<T> Check<T>(
            this IGen<Property.Test<T>> property,
            int? iterations = null,
            int? seed = null,
            int? size = null,
            int? shrinkLimit = null,
            string? replay = null,
            bool deepCheck = true,
            TimeSpan? timeout = null,
            bool exhaustive = true,
            HealthCheck[]? suppressHealthChecks = null)
        {
            var options = new RunOptions(timeout, exhaustive, suppressHealthChecks);
            return CheckRunner.Run(property, iterations, seed, size, shrinkLimit, replay, deepCheck, recordChecks: false, options);
        }

        /// <summary>Runs the async property and returns the result, with the shrunk counterexample of a failure.</summary>
        public static Task<CheckResult<T>> CheckAsync<T>(
            this IGen<Property.AsyncTest<T>> property,
            int? iterations = null,
            int? seed = null,
            int? size = null,
            int? shrinkLimit = null,
            string? replay = null,
            bool deepCheck = true,
            TimeSpan? timeout = null,
            bool exhaustive = true,
            HealthCheck[]? suppressHealthChecks = null)
        {
            var options = new RunOptions(timeout, exhaustive, suppressHealthChecks);
            return CheckRunner.RunAsync(property, iterations, seed, size, shrinkLimit, replay, deepCheck, recordChecks: false, options);
        }
    }
}

namespace GalaxyCheck.Runners
{
    /// <summary>
    /// The result of one test case.
    /// </summary>
    internal struct TestCaseOutcome<T>
    {
        public bool Failed;
        public Exception? Exception;
        public T Input;
        public IReadOnlyList<object?>? PresentedInput;

        /// <summary>True when a precondition of the test function was false. The test case does not count.</summary>
        public bool Discarded;

        /// <summary>True when the test case ran over the time limit.</summary>
        public bool TimedOut;

        /// <summary>The milliseconds that the generation took. Zero when the runner does not measure it.</summary>
        public long GenerationMs;
    }

    internal static class CheckRunner
    {
        public const int DefaultIterations = 100;
        public const int DefaultShrinkLimit = 500;

        /// <summary>
        /// The size of each iteration. With 100 or more iterations, the size grows by one each iteration and wraps
        /// at 100. With fewer iterations, the sizes spread from 0 to 100.
        /// </summary>
        public static int SizeOf(int iteration, int iterations, int? size)
        {
            if (size != null) return size.Value;
            if (iterations >= 100) return iteration % 100;

            var samples = Math.Min(iterations, 101);
            if (iteration >= samples - 1) return 100;
            return iteration * 100 / (samples - 1);
        }

        /// <summary>The seed option, else the seed of the environment (<see cref="AgentMode"/>), else a random seed.</summary>
        public static int CreateSeed(int? seed) => seed ?? AgentMode.DefaultSeed ?? Random.Shared.Next();

        public static Exceptions.GenErrorException GenError(Exception ex, GenParameters parameters) => ex switch
        {
            GenErrorSignal signal => new Exceptions.GenErrorException("Error during generation: " + signal.Message, parameters),
            _ => new Exceptions.GenErrorException($"Error during generation: Exception thrown.{Environment.NewLine}{Environment.NewLine}{ex}", parameters)
        };

        /// <summary>
        /// True for an exception that a generator throws on purpose and that the runner passes on.
        /// </summary>
        public static bool IsPassedOn(Exception ex) =>
            ex is Exceptions.GenExhaustionException || ex is Exceptions.GenLimitExceededException || ex is Exceptions.GenErrorException;

        /// <summary>A failed test case whose exception is a false precondition is a discard, not a failure.</summary>
        private static void Discard<T>(ref TestCaseOutcome<T> outcome)
        {
            if (outcome.Failed && outcome.Exception is PreconditionFailedException)
            {
                outcome.Failed = false;
                outcome.Exception = null;
                outcome.Discarded = true;
            }
        }

        private static HealthCheckException ExhaustionHealthCheck() => HealthMessages.Exception(
            HealthCheck.TooManyDiscards,
            "the generator could not make a value, because it discarded too many values in a row.");

        /// <summary>
        /// The outcome of a test case whose generator could not make a value while the discard check is suppressed:
        /// the test case is discarded.
        /// </summary>
        private static TestCaseOutcome<T> Exhausted<T>() => new TestCaseOutcome<T> { Discarded = true };

        public static CheckResult<T> Run<T>(
            IGen<Property.Test<T>> property,
            int? iterations,
            int? seed,
            int? size,
            int? shrinkLimit,
            string? replay,
            bool deepCheck,
            bool recordChecks,
            RunOptions options = default)
        {
            var gen = property.Advanced;
            var timeoutMs = options.TimeoutMilliseconds;
            var fast = timeoutMs == 0 ? gen as IFastProperty<T> : null;
            var track = options.Watches(HealthCheck.SlowGenerator);

            TestCaseOutcome<T> Evaluate(GenSource source)
            {
                var outcome = new TestCaseOutcome<T>();
                if (fast != null)
                {
                    var start = track ? Environment.TickCount64 : 0;
                    outcome.Input = fast.Run(source, track, out var generatedAt, out outcome.Failed, out outcome.Exception);
                    if (track) outcome.GenerationMs = generatedAt - start;
                    if (!outcome.Failed && outcome.Exception != null)
                    {
                        outcome.Discarded = true;
                        outcome.Exception = null;
                    }
                }
                else
                {
                    var start = track ? Environment.TickCount64 : 0;
                    var test = gen.Generate(source);
                    if (track) outcome.GenerationMs = Environment.TickCount64 - start;
                    outcome.Input = test.Input;
                    outcome.PresentedInput = test.PresentedInput;

                    if (!source.ShouldRunTest())
                    {
                        return outcome;
                    }

                    if (timeoutMs == 0)
                    {
                        var output = test.Output.Value;
                        outcome.Failed = output.Result == Property.TestResult.Failed;
                        outcome.Exception = output.Exception;
                    }
                    else
                    {
                        var output = Timeouts.RunSync(test.Output, timeoutMs, out var error);
                        outcome.Failed = output == null || output.Result == Property.TestResult.Failed;
                        outcome.Exception = output == null ? error : output.Exception;
                        outcome.TimedOut = error is TestTimeoutException;
                    }

                    Discard(ref outcome);
                }

                return outcome;
            }

            IReadOnlyList<object?> Present(in TestCaseOutcome<T> outcome) =>
                outcome.PresentedInput ?? fast!.Present(outcome.Input);

            var run = new CheckRun<T>(iterations, seed, size, shrinkLimit, replay, deepCheck, recordChecks, options, (property as Property<T>)?.Rules, gen as IFiniteGen);
            var source = run.Source;

            while (run.NextTestCase())
            {
                TestCaseOutcome<T> outcome;
                try
                {
                    outcome = Evaluate(source);
                }
                catch (Exceptions.GenExhaustionException) when (source.IsWalking)
                {
                    // A filter discards a value of the walk of an exhaustive run.
                    outcome = Exhausted<T>();
                }
                catch (Exceptions.GenExhaustionException) when (options.Watches(HealthCheck.TooManyDiscards))
                {
                    throw ExhaustionHealthCheck();
                }
                catch (Exceptions.GenExhaustionException) when (options.HealthChecks)
                {
                    outcome = Exhausted<T>();
                }
                catch (Exception ex) when (!IsPassedOn(ex))
                {
                    throw GenError(ex, run.Parameters);
                }
                finally
                {
                    source.End();
                }

                if (run.Complete(outcome, recordChecks ? Present(outcome) : null))
                {
                    break;
                }
            }

            run.Finish();

            if (run.Shrinker != null)
            {
                foreach (var _ in run.Shrinker.Candidates())
                {
                    run.BeginCandidate();
                    TestCaseOutcome<T> outcome;
                    try
                    {
                        outcome = Evaluate(source);
                    }
                    catch (Exception)
                    {
                        // A candidate that the generator cannot build is not a counterexample.
                        continue;
                    }
                    finally
                    {
                        source.End();
                    }

                    run.CompleteCandidate(outcome);
                }
            }

            return run.Result(outcome => Present(outcome));
        }

        public static async Task<CheckResult<T>> RunAsync<T>(
            IGen<Property.AsyncTest<T>> property,
            int? iterations,
            int? seed,
            int? size,
            int? shrinkLimit,
            string? replay,
            bool deepCheck,
            bool recordChecks,
            RunOptions options = default)
        {
            var gen = property.Advanced;
            var timeoutMs = options.TimeoutMilliseconds;
            var track = options.Watches(HealthCheck.SlowGenerator);

            async Task<TestCaseOutcome<T>> Evaluate(GenSource source)
            {
                var start = track ? Environment.TickCount64 : 0;
                var test = gen.Generate(source);
                var generationMs = track ? Environment.TickCount64 - start : 0;

                if (!source.ShouldRunTest())
                {
                    return new TestCaseOutcome<T> { Input = test.Input, PresentedInput = test.PresentedInput };
                }

                Property.TestOutput? output;
                Exception? error = null;
                if (timeoutMs == 0)
                {
                    output = await test.Output.Value;
                }
                else
                {
                    (output, error) = await Timeouts.RunAsync(test.Output, timeoutMs);
                }

                var outcome = new TestCaseOutcome<T>
                {
                    Failed = output == null || output.Result == Property.TestResult.Failed,
                    Exception = output == null ? error : output.Exception,
                    Input = test.Input,
                    PresentedInput = test.PresentedInput,
                    GenerationMs = generationMs,
                    TimedOut = error is TestTimeoutException
                };

                Discard(ref outcome);
                return outcome;
            }

            var run = new CheckRun<T>(iterations, seed, size, shrinkLimit, replay, deepCheck, recordChecks, options, (property as AsyncProperty<T>)?.Rules, gen as IFiniteGen);
            var source = run.Source;

            while (run.NextTestCase())
            {
                TestCaseOutcome<T> outcome;
                try
                {
                    outcome = await Evaluate(source);
                }
                catch (Exceptions.GenExhaustionException) when (source.IsWalking)
                {
                    // A filter discards a value of the walk of an exhaustive run.
                    outcome = Exhausted<T>();
                }
                catch (Exceptions.GenExhaustionException) when (options.Watches(HealthCheck.TooManyDiscards))
                {
                    throw ExhaustionHealthCheck();
                }
                catch (Exceptions.GenExhaustionException) when (options.HealthChecks)
                {
                    outcome = Exhausted<T>();
                }
                catch (Exception ex) when (!IsPassedOn(ex))
                {
                    throw GenError(ex, run.Parameters);
                }
                finally
                {
                    source.End();
                }

                if (run.Complete(outcome, recordChecks ? outcome.PresentedInput : null))
                {
                    break;
                }
            }

            run.Finish();

            if (run.Shrinker != null)
            {
                foreach (var _ in run.Shrinker.Candidates())
                {
                    run.BeginCandidate();
                    TestCaseOutcome<T> outcome;
                    try
                    {
                        outcome = await Evaluate(source);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                    finally
                    {
                        source.End();
                    }

                    run.CompleteCandidate(outcome);
                }
            }

            return run.Result(outcome => outcome.PresentedInput!);
        }
    }

    /// <summary>
    /// The state of one check: the iterations, the first counterexample, and its shrinking.
    /// </summary>
    internal sealed class CheckRun<T>
    {
        private const long SlowGeneratorMs = 1000;
        private int _slowValues;
        private const long EarlyDiscardTotal = 1000;

        private readonly int _iterations;
        private readonly int _seed;
        private readonly int? _size;
        private readonly int _shrinkLimit;
        private readonly Replay? _replay;
        private readonly bool _deepCheck;
        private readonly List<CheckIteration<T>>? _checks;
        private readonly RunOptions _options;
        private readonly bool _watchSlow;
        private readonly bool _watchDiscards;
        private readonly long _maxPreDiscards;

        private readonly IReadOnlyList<LabelRule<T>>? _rules;
        private readonly int[]? _labelCounts;
        private readonly string[]? _labelNames;
        private readonly int[]? _ruleGroup;
        private readonly bool[]? _groupSeen;

        private readonly IFiniteGen? _finite;
        private readonly long _finiteCount;
        private readonly List<ulong>? _finiteChoices;
        private ulong[] _finiteBuffer = Array.Empty<ulong>();

        private int _iteration;
        private long _finiteIndex;
        private int _preDiscards;
        private int _retry;
        private int _goodSize = SizeSearch.NoGoodSize;
        private bool _replayStarted;
        private int _currentSize;
        private TestCaseOutcome<T> _counterexample;
        private bool _hasCounterexample;
        private int _counterexampleSize;

        public CheckRun(
            int? iterations,
            int? seed,
            int? size,
            int? shrinkLimit,
            string? replay,
            bool deepCheck,
            bool recordChecks,
            RunOptions options = default,
            IReadOnlyList<LabelRule<T>>? rules = null,
            IFiniteGen? finite = null)
        {
            _iterations = iterations ?? CheckRunner.DefaultIterations;
            _size = size;
            _shrinkLimit = shrinkLimit ?? CheckRunner.DefaultShrinkLimit;
            _deepCheck = deepCheck;
            _checks = recordChecks ? new List<CheckIteration<T>>(Math.Max(0, Math.Min(_iterations, 10_000))) : null;
            _options = options;
            _watchSlow = options.Watches(HealthCheck.SlowGenerator);
            _watchDiscards = options.Watches(HealthCheck.TooManyDiscards) || options.Watches(HealthCheck.TestFunctionNeverRuns);
            _maxPreDiscards = Math.Max(1000L, 10L * Math.Max(0, _iterations));

            if (rules != null && rules.Count > 0 && replay == null)
            {
                _rules = rules;
                _labelNames = rules.Select(rule => rule.Label).Distinct().ToArray();
                _ruleGroup = rules.Select(rule => Array.IndexOf(_labelNames, rule.Label)).ToArray();
                _labelCounts = new int[_labelNames.Length];
                _groupSeen = new bool[_labelNames.Length];
            }

            if (replay != null)
            {
                try
                {
                    _replay = ReplayEncoding.Decode(replay);
                }
                catch (FormatException ex)
                {
                    throw new Exceptions.GenErrorException($"Error decoding replay string: {ex.Message}", null);
                }

                _seed = _replay.Seed;
            }
            else
            {
                _seed = CheckRunner.CreateSeed(seed);

                if (options.Exhaustive && finite != null && _iterations > 0)
                {
                    var count = finite.Count(_iterations);
                    if (count != null && count.Value > 0)
                    {
                        _finite = finite;
                        _finiteCount = count.Value;
                        _finiteChoices = new List<ulong>();
                    }
                }
            }

            _currentSize = _size ?? CheckRunner.SizeOf(0, _iterations, null);
            Source = GenSource.Create(_seed, _currentSize);
            Source.SizeFixed = _size != null;
            Source.IsWalking = _finite != null;
        }

        public GenSource Source { get; }

        public Shrinker? Shrinker { get; private set; }

        public GenParameters Parameters => GenParameters.Parse(_seed, _currentSize);

        public bool NextTestCase()
        {
            if (_hasCounterexample) return false;

            if (_replay != null)
            {
                if (_replayStarted) return false;
                _replayStarted = true;
                _currentSize = _replay.Size;
                Source.BeginReplay(_replay.Choices, _replay.Choices.Length, _currentSize);
                return true;
            }

            if (_preDiscards > _maxPreDiscards) return false;

            if (_finite != null)
            {
                // The walk advances on each test case, discarded or not, so each value of the domain runs one time.
                if (_finiteIndex >= _finiteCount) return false;

                _currentSize = CheckRunner.SizeOf((int)_finiteIndex, _iterations, _size);
                _finiteChoices!.Clear();
                _finite.WriteChoices(_finiteIndex, _finiteChoices);
                _finiteIndex++;
                if (_finiteBuffer.Length < _finiteChoices.Count)
                {
                    _finiteBuffer = new ulong[Math.Max(8, _finiteChoices.Count)];
                }

                _finiteChoices.CopyTo(_finiteBuffer);
                Source.BeginReplay(_finiteBuffer, _finiteChoices.Count, _currentSize);
                return true;
            }

            if (_iteration >= _iterations) return false;

            _currentSize = CheckRunner.SizeOf(_iteration, _iterations, _size);
            if (_retry != 0 && _size == null)
            {
                // A test case after a discarded one retries the same place of the plan at another size, as a filter
                // in a generator does. The replay string records the size of the test case.
                _currentSize = SizeSearch.Next(_currentSize, _retry, _goodSize);
            }

            Source.BeginRandom(_currentSize);
            return true;
        }

        /// <summary>
        /// Records the outcome of a test case. Returns true when the iterations stop.
        /// </summary>
        public bool Complete(in TestCaseOutcome<T> outcome, IReadOnlyList<object?>? presentedInput)
        {
            // The clock also counts a pause of the machine (a first JIT compile, a garbage collection, a busy build
            // agent). Such a pause does not repeat, and a slow generator does, so the second slow value trips the check.
            if (_watchSlow && outcome.GenerationMs > SlowGeneratorMs && ++_slowValues >= 2)
            {
                throw HealthMessages.Exception(
                    HealthCheck.SlowGenerator,
                    $"one value took {outcome.GenerationMs} ms to generate, and the limit is {SlowGeneratorMs} ms. It is the second slow value of this run.");
            }

            if (outcome.Discarded)
            {
                _preDiscards++;
                _retry++;
                if (_watchDiscards) CheckHealth(false);
                return false;
            }

            _iteration++;

            if (_retry != 0)
            {
                _goodSize = SizeSearch.Learn(_goodSize, _currentSize);
                _retry = 0;
            }

            if (_replay != null && !ReplayFits())
            {
                throw new Exceptions.GenErrorException(
                    "Error replaying last example, given replay string was no longer valid. Remove the replay " +
                    "parameter and run the property again. This is probably due to the generator setup changing.",
                    Parameters);
            }

            if (_checks != null)
            {
                _checks.Add(new CheckIteration<T>(
                    presentedInput!,
                    outcome.Input,
                    Parameters,
                    outcome.Failed ? outcome.Exception : null));
            }

            if (_rules != null)
            {
                CountLabels(outcome);
            }

            if (!outcome.Failed) return false;

            _counterexample = outcome;
            _hasCounterexample = true;
            _counterexampleSize = _currentSize;

            // A test case over the time limit does not shrink: each candidate could wait for the limit again.
            var limit = _replay != null || outcome.TimedOut ? 0 : _shrinkLimit;
            Shrinker = new Shrinker(Source, limit);
            Source.IsWalking = false;
            Source.Shrinker = Shrinker;

            return true;
        }

        private void CountLabels(in TestCaseOutcome<T> outcome)
        {
            Array.Clear(_groupSeen!);
            for (var i = 0; i < _rules!.Count; i++)
            {
                var group = _ruleGroup![i];
                if (!_groupSeen![group] && _rules[i].When(outcome.Input))
                {
                    _groupSeen[group] = true;
                    _labelCounts![group]++;
                }
            }
        }

        /// <summary>Checks the health of the run at the end of the iterations.</summary>
        public void Finish()
        {
            if (_hasCounterexample || _replay != null) return;
            if (_watchDiscards) CheckHealth(true);
        }

        /// <summary>
        /// Checks the discards. In the middle of the run, a run in which no test case reached the test function stops
        /// early, and so does a run that discarded more than 98% of at least 1000 values. Else the share of discards is
        /// judged at the end, over the whole run. The filters of the generators and the preconditions each have their
        /// own share: a filter discards a generated value, and a precondition discards a test case.
        /// </summary>
        private void CheckHealth(bool final)
        {
            long tests = _iteration;

            if (tests == 0 && _preDiscards > 0)
            {
                if ((final || _preDiscards >= 100) && _options.Watches(HealthCheck.TestFunctionNeverRuns))
                {
                    throw HealthMessages.Exception(
                        HealthCheck.TestFunctionNeverRuns,
                        $"no test case reached the test function: each of {_preDiscards} test cases was discarded.");
                }

                return;
            }

            if (_options.Watches(HealthCheck.TooManyDiscards))
            {
                CheckShare(final, Source.Discards, Source.Discards + (long)Source.Kept);
                CheckShare(final, _preDiscards, _preDiscards + tests);
            }
        }

        private static void CheckShare(bool final, long discards, long total)
        {
            if (discards == 0) return;

            var tooMany = final
                ? total >= 20 && discards * 10 > total * 9
                : total >= EarlyDiscardTotal && discards * 50 > total * 49;
            if (tooMany)
            {
                throw HealthMessages.Exception(
                    HealthCheck.TooManyDiscards,
                    $"the run discarded {discards} of {total} generated values, which is more than 90%.");
            }
        }

        /// <summary>
        /// A replay fits when the generators read exactly the choices of the replay. Else the generators changed.
        /// </summary>
        private bool ReplayFits()
        {
            var choices = _replay!.Choices;
            if (Source.Length != choices.Length) return false;

            for (var i = 0; i < choices.Length; i++)
            {
                if (Source.Choices[i] != choices[i]) return false;
            }

            return true;
        }

        public void BeginCandidate()
        {
            Source.BeginReplay(Shrinker!.Candidate, Shrinker.CandidateLength, _counterexampleSize);
        }

        public void CompleteCandidate(in TestCaseOutcome<T> outcome)
        {
            if (outcome.Failed && Shrinker!.TryAccept(Source))
            {
                _counterexample = outcome;
            }
        }

        public CheckResult<T> Result(Func<TestCaseOutcome<T>, IReadOnlyList<object?>> present)
        {
            var initialParameters = GenParameters.Parse(_seed, _size ?? CheckRunner.SizeOf(0, _iterations, null));
            var nextParameters = GenParameters.Parse(_seed, _currentSize);
            IReadOnlyCollection<CheckIteration<T>> checks = _checks ?? (IReadOnlyCollection<CheckIteration<T>>)Array.Empty<CheckIteration<T>>();
            var discarded = Source.Discards + _preDiscards;
            var shares = LabelShares();

            if (!_hasCounterexample)
            {
                var coverFailure = CoverFailure(shares);
                if (coverFailure != null)
                {
                    // A coverage failure has no counterexample and no replay: no test case failed.
                    return new CheckResult<T>(
                        _iteration,
                        discarded,
                        0,
                        null,
                        checks,
                        initialParameters,
                        nextParameters,
                        TerminationReason.ReachedMaximumIterations)
                    {
                        RandomnessConsumption = (int)Math.Min(int.MaxValue, Source.Consumption),
                        LabelShares = shares,
                        IsExhaustive = _finite != null,
                        ExhaustiveCount = _finite != null ? _finiteCount : 0,
                        CoverageFailure = coverFailure
                    };
                }

                return new CheckResult<T>(
                    _iteration,
                    discarded,
                    0,
                    null,
                    checks,
                    initialParameters,
                    nextParameters,
                    _replay != null ? TerminationReason.IsReplay : TerminationReason.ReachedMaximumIterations)
                {
                    RandomnessConsumption = (int)Math.Min(int.MaxValue, Source.Consumption),
                    LabelShares = shares,
                    IsExhaustive = _finite != null,
                    ExhaustiveCount = _finite != null ? _finiteCount : 0
                };
            }

            var shrinker = Shrinker!;
            var replayParameters = GenParameters.Parse(_seed, _counterexampleSize);
            var counterexample = new Counterexample<T>(
                _counterexample.Input,
                replayParameters,
                ReplayEncoding.Encode(_seed, _counterexampleSize, shrinker.Best, shrinker.BestLength),
                _counterexample.Exception,
                present(_counterexample));

            var terminationReason =
                _replay != null ? TerminationReason.IsReplay :
                shrinker.ReachedShrinkLimit ? TerminationReason.ReachedShrinkLimit :
                !_deepCheck ? TerminationReason.DeepCheckDisabled :
                IsZero(shrinker) ? TerminationReason.FoundTheoreticalSmallestCounterexample :
                TerminationReason.FoundPragmaticSmallestCounterexample;

            return new CheckResult<T>(
                _iteration,
                discarded,
                shrinker.Shrinks,
                counterexample,
                checks,
                initialParameters,
                nextParameters,
                terminationReason)
            {
                RandomnessConsumption = (int)Math.Min(int.MaxValue, Source.Consumption),
                LabelShares = shares,
                IsExhaustive = _finite != null,
                ExhaustiveCount = _finite != null ? _finiteCount : 0
            };
        }

        private IReadOnlyDictionary<string, double>? LabelShares()
        {
            if (_rules == null || _iteration == 0) return null;

            var shares = new Dictionary<string, double>();
            for (var i = 0; i < _labelNames!.Length; i++)
            {
                shares[_labelNames[i]] = 100.0 * _labelCounts![i] / _iteration;
            }

            return shares;
        }

        private CoverageFailure? CoverFailure(IReadOnlyDictionary<string, double>? shares)
        {
            if (_rules == null || shares == null || _replay != null) return null;

            foreach (var rule in _rules)
            {
                if (rule.MinPercent is { } required && shares[rule.Label] < required)
                {
                    var actual = shares[rule.Label];
                    return new CoverageFailure(
                        rule.Label,
                        required,
                        actual,
                        $"Cover not met: label \"{rule.Label}\" requires {PercentFormat.Format(required)} of the test cases, " +
                        $"but only {PercentFormat.Format(actual)} ({Math.Round(actual * _iteration / 100.0)} of {_iteration}) had it.");
                }
            }

            return null;
        }

        private static bool IsZero(Shrinker shrinker)
        {
            for (var i = 0; i < shrinker.BestLength; i++)
            {
                if (shrinker.Best[i] != 0) return false;
            }

            return true;
        }
    }

    internal static class PercentFormat
    {
        public static string Format(double percent) =>
            Math.Round(percent, 1).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "%";
    }
}
