using GalaxyCheck.Gens.Internal;
using GalaxyCheck.Gens.Parameters;
using GalaxyCheck.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace GalaxyCheck.Stateful
{
    /// <summary>A command that the report shows by its <c>ToString()</c>.</summary>
    internal interface IStatefulCommand
    {
    }

    /// <summary>One step of a stateful test, against a real system and a model of it.</summary>
    public abstract class Command<TSystem, TModel> : IStatefulCommand
    {
        public virtual bool Precondition(TModel model) => true;

        public abstract void Run(TSystem system);

        public abstract TModel NextModel(TModel model);

        public virtual bool Postcondition(TSystem system, TModel modelBefore, TModel modelAfter) => true;

        public virtual bool Expects(Exception exception) => false;
    }

    public static class StatefulGen
    {
        public static IGen<IReadOnlyList<Command<TSystem, TModel>>> Commands<TSystem, TModel>(
            Func<TModel> newModel,
            Func<TModel, IGen<Command<TSystem, TModel>>> nextCommand)
        {
            if (newModel == null) throw new ArgumentNullException(nameof(newModel));
            if (nextCommand == null) throw new ArgumentNullException(nameof(nextCommand));
            return new CommandsGen<TSystem, TModel>(newModel, nextCommand);
        }
    }

    public static class StatefulProperty
    {
        public static Property<IReadOnlyList<Command<TSystem, TModel>>> Create<TSystem, TModel>(
            Func<TSystem> newSystem,
            Func<TModel> newModel,
            Func<TModel, IGen<Command<TSystem, TModel>>> nextCommand)
        {
            if (newSystem == null) throw new ArgumentNullException(nameof(newSystem));

            return new Property<IReadOnlyList<Command<TSystem, TModel>>>(
                new ForAllGen<IReadOnlyList<Command<TSystem, TModel>>>(
                    StatefulGen.Commands(newModel, nextCommand),
                    commands => StatefulRun.Run(newSystem, newModel, commands),
                    commands => new object?[] { commands }));
        }
    }

    /// <summary>
    /// The sequence generator. Each command is one span (the choice "one more command" and the choices of the
    /// command), and a choice of 0 ends the sequence, as in a list. A command is drawn from the model at its position,
    /// and a command whose precondition is false there is never kept. The shrinker replays edited choices through
    /// this generator, so each shrunk sequence holds each precondition too (I6, StatefulRun.X1).
    /// </summary>
    internal sealed record CommandsGen<TSystem, TModel>(
        Func<TModel> NewModel,
        Func<TModel, IGen<Command<TSystem, TModel>>> NextCommand) : BaseGen<IReadOnlyList<Command<TSystem, TModel>>>
    {
        private const int MaxCount = 20;

        /// <summary>The draws at one position before the position stays empty.</summary>
        private const int MaxAttempts = 10;

        public override IReadOnlyList<Command<TSystem, TModel>> Generate(GenSource source)
        {
            var model = NewModel();
            var commands = new List<Command<TSystem, TModel>>();

            if (!source.IsReplaying)
            {
                var count = IntCore.RandomInt32(source, 0, MaxCount, Gen.Bias.WithSize);
                for (var i = 0; i < count; i++)
                {
                    var start = source.BeginSpan();
                    source.Record(1);
                    model = Draw(source, model, commands);
                    source.EndSpan(start);
                }

                if (count < MaxCount)
                {
                    source.Record(0);
                    source.MarkStop();
                }

                return new CommandSequence<TSystem, TModel>(commands);
            }

            for (var i = 0; i < MaxCount; i++)
            {
                var start = source.BeginSpan();
                if (source.Read(1) == 0)
                {
                    source.MarkStop();
                    break;
                }

                model = Draw(source, model, commands);
                source.EndSpan(start);
            }

            return new CommandSequence<TSystem, TModel>(commands);
        }

        private TModel Draw(GenSource source, TModel model, List<Command<TSystem, TModel>> commands)
        {
            var gen = NextCommand(model) ?? throw new GenErrorSignal("'nextCommand' returned null");
            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var command = gen.Advanced.Generate(source) ?? throw new GenErrorSignal("'nextCommand' generated a null command");
                if (command.Precondition(model))
                {
                    commands.Add(command);
                    return command.NextModel(model);
                }
            }

            return model;
        }
    }

    /// <summary>A generated sequence of commands. It has no C# expression, and its text lists the commands.</summary>
    internal sealed class CommandSequence<TSystem, TModel> : IReadOnlyList<Command<TSystem, TModel>>
    {
        private readonly List<Command<TSystem, TModel>> _commands;

        internal CommandSequence(List<Command<TSystem, TModel>> commands)
        {
            _commands = commands;
        }

        public Command<TSystem, TModel> this[int index] => _commands[index];

        public int Count => _commands.Count;

        public IEnumerator<Command<TSystem, TModel>> GetEnumerator() => _commands.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString() => "[" + string.Join(", ", _commands) + "]";
    }

    /// <summary>One run of a sequence against a new system and a new model (the machine StatefulRun).</summary>
    internal static class StatefulRun
    {
        public static bool Run<TSystem, TModel>(
            Func<TSystem> newSystem,
            Func<TModel> newModel,
            IReadOnlyList<Command<TSystem, TModel>> commands)
        {
            // T1: a new system and a new model.
            var system = newSystem();
            var model = newModel();

            for (var i = 0; i < commands.Count; i++)
            {
                var command = commands[i];

                // X1: a command whose precondition is false never runs. The generator does not place one, so only a
                // model that is not deterministic gets here, and the sequence is discarded.
                if (!command.Precondition(model))
                {
                    throw PreconditionFailedException.Instance;
                }

                try
                {
                    command.Run(system);
                }
                catch (Exception ex) when (!command.Expects(ex))
                {
                    // T3: an exception that the command does not expect.
                    throw new StatefulCommandFailedException(
                        $"Command {i + 1} threw an exception that it does not expect: {ex.GetType().Name}: {ex.Message}",
                        commands,
                        i,
                        ex);
                }

                var next = command.NextModel(model);
                if (!command.Postcondition(system, model, next))
                {
                    // T3: a postcondition fails.
                    throw new StatefulCommandFailedException($"The postcondition of command {i + 1} failed", commands, i, null);
                }

                // T2: change the model.
                model = next;
            }

            // T4: the sequence passed.
            return true;
        }
    }

    /// <summary>The failure of a stateful run. Its message lists the commands and marks the command that failed.</summary>
    internal sealed class StatefulCommandFailedException : Exception
    {
        public StatefulCommandFailedException(string reason, IEnumerable commands, int failedIndex, Exception? inner)
            : base(BuildMessage(reason, commands, failedIndex), inner)
        {
        }

        private static string BuildMessage(string reason, IEnumerable commands, int failedIndex)
        {
            var builder = new StringBuilder(reason).Append('.').AppendLine().Append("Commands:");
            var index = 0;
            foreach (var command in commands)
            {
                builder.AppendLine().Append(index + 1).Append(". ").Append(command);
                if (index == failedIndex)
                {
                    builder.Append(" <- failed");
                }

                index++;
            }

            return builder.ToString();
        }
    }
}
