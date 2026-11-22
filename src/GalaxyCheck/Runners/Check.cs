using GalaxyCheck.Gens.Internal;
using GalaxyCheck.Gens.Parameters;
using GalaxyCheck.Properties;
using GalaxyCheck.Runners;
using GalaxyCheck.Runners.Check;
using GalaxyCheck.Runners.Replaying;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GalaxyCheck
{
    public static partial class Extensions
    {
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
            GalaxyCheck.Runners.CheckOptionsStub.ThrowIfUsed(timeout, exhaustive, suppressHealthChecks);
            return CheckRunner.Run(property, iterations, seed, size, shrinkLimit, replay, deepCheck, recordChecks: false);
        }

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
            GalaxyCheck.Runners.CheckOptionsStub.ThrowIfUsed(timeout, exhaustive, suppressHealthChecks);
            return CheckRunner.RunAsync(property, iterations, seed, size, shrinkLimit, replay, deepCheck, recordChecks: false);
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

        public static int CreateSeed(int? seed) => seed ?? Random.Shared.Next();

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

        public static CheckResult<T> Run<T>(
            IGen<Property.Test<T>> property,
            int? iterations,
            int? seed,
            int? size,
            int? shrinkLimit,
            string? replay,
            bool deepCheck,
            bool recordChecks)
        {
            var gen = property.Advanced;
            var fast = gen as IFastProperty<T>;

            TestCaseOutcome<T> Evaluate(GenSource source)
            {
                var outcome = new TestCaseOutcome<T>();
                if (fast != null)
                {
                    outcome.Input = fast.Run(source, out outcome.Failed, out outcome.Exception);
                }
                else
                {
                    var test = gen.Generate(source);
                    var output = test.Output.Value;
                    outcome.Failed = output.Result == Property.TestResult.Failed;
                    outcome.Exception = output.Exception;
                    outcome.Input = test.Input;
                    outcome.PresentedInput = test.PresentedInput;
                }

                return outcome;
            }

            IReadOnlyList<object?> Present(in TestCaseOutcome<T> outcome) =>
                outcome.PresentedInput ?? fast!.Present(outcome.Input);

            var run = new CheckRun<T>(iterations, seed, size, shrinkLimit, replay, deepCheck, recordChecks);
            var source = run.Source;

            while (run.NextTestCase())
            {
                TestCaseOutcome<T> outcome;
                try
                {
                    outcome = Evaluate(source);
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
            bool recordChecks)
        {
            var gen = property.Advanced;

            async Task<TestCaseOutcome<T>> Evaluate(GenSource source)
            {
                var test = gen.Generate(source);
                var output = await test.Output.Value;
                return new TestCaseOutcome<T>
                {
                    Failed = output.Result == Property.TestResult.Failed,
                    Exception = output.Exception,
                    Input = test.Input,
                    PresentedInput = test.PresentedInput
                };
            }

            var run = new CheckRun<T>(iterations, seed, size, shrinkLimit, replay, deepCheck, recordChecks);
            var source = run.Source;

            while (run.NextTestCase())
            {
                TestCaseOutcome<T> outcome;
                try
                {
                    outcome = await Evaluate(source);
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
        private readonly int _iterations;
        private readonly int _seed;
        private readonly int? _size;
        private readonly int _shrinkLimit;
        private readonly Replay? _replay;
        private readonly bool _deepCheck;
        private readonly List<CheckIteration<T>>? _checks;

        private int _iteration;
        private int _currentSize;
        private TestCaseOutcome<T> _counterexample;
        private bool _hasCounterexample;
        private int _counterexampleSize;

        public CheckRun(int? iterations, int? seed, int? size, int? shrinkLimit, string? replay, bool deepCheck, bool recordChecks)
        {
            _iterations = iterations ?? CheckRunner.DefaultIterations;
            _size = size;
            _shrinkLimit = shrinkLimit ?? CheckRunner.DefaultShrinkLimit;
            _deepCheck = deepCheck;
            _checks = recordChecks ? new List<CheckIteration<T>>(Math.Max(0, Math.Min(_iterations, 10_000))) : null;

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
            }

            _currentSize = _size ?? CheckRunner.SizeOf(0, _iterations, null);
            Source = GenSource.Create(_seed, _currentSize);
        }

        public GenSource Source { get; }

        public Shrinker? Shrinker { get; private set; }

        public GenParameters Parameters => GenParameters.Parse(_seed, _currentSize);

        public bool NextTestCase()
        {
            if (_hasCounterexample) return false;

            if (_replay != null)
            {
                if (_iteration > 0) return false;
                _currentSize = _replay.Size;
                Source.BeginReplay(_replay.Choices, _replay.Choices.Length, _currentSize);
                return true;
            }

            if (_iteration >= _iterations) return false;

            _currentSize = CheckRunner.SizeOf(_iteration, _iterations, _size);
            Source.BeginRandom(_currentSize);
            return true;
        }

        /// <summary>
        /// Records the outcome of a test case. Returns true when the iterations stop.
        /// </summary>
        public bool Complete(in TestCaseOutcome<T> outcome, IReadOnlyList<object?>? presentedInput)
        {
            _iteration++;

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

            if (!outcome.Failed) return false;

            _counterexample = outcome;
            _hasCounterexample = true;
            _counterexampleSize = _currentSize;
            Shrinker = new Shrinker(Source, _replay == null ? _shrinkLimit : 0);

            return true;
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

            if (!_hasCounterexample)
            {
                return new CheckResult<T>(
                    _iteration,
                    Source.Discards,
                    0,
                    null,
                    checks,
                    initialParameters,
                    nextParameters,
                    _replay != null ? TerminationReason.IsReplay : TerminationReason.ReachedMaximumIterations)
                {
                    RandomnessConsumption = (int)Math.Min(int.MaxValue, Source.Consumption)
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
                Source.Discards,
                shrinker.Shrinks,
                counterexample,
                checks,
                initialParameters,
                nextParameters,
                terminationReason)
            {
                RandomnessConsumption = (int)Math.Min(int.MaxValue, Source.Consumption)
            };
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
}
