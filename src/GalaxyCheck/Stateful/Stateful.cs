using GalaxyCheck.Gens.Internal;
using GalaxyCheck.Gens.Parameters;
using GalaxyCheck.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GalaxyCheck.Stateful
{
    /// <summary>A command that the report shows by its <c>ToString()</c>.</summary>
    internal interface IStatefulCommand
    {
    }

    /// <summary>
    /// One step of a stateful test, against a real system and a model of it. The report shows a command by its
    /// <c>ToString()</c>, on one line (see <see cref="CommandText"/>).
    /// </summary>
    public abstract class Command<TSystem, TModel> : IStatefulCommand
    {
        /// <summary>True if the command may run when the model is <paramref name="model"/>. The generator and the
        /// shrinker never place a command where this is false.</summary>
        public virtual bool Precondition(TModel model) => true;

        /// <summary>Runs the command against the real system.</summary>
        public abstract void Run(TSystem system);

        /// <summary>Returns the model after the command. It returns a new model and does not change its argument,
        /// because <see cref="Postcondition"/> gets the model before and the model after.</summary>
        public abstract TModel NextModel(TModel model);

        /// <summary>True if the system agrees with the model after the command.</summary>
        public virtual bool Postcondition(TSystem system, TModel modelBefore, TModel modelAfter) => true;

        /// <summary>True if <see cref="Run"/> may throw <paramref name="exception"/>. The run then goes on: the model
        /// advances with <see cref="NextModel"/> and the postcondition is checked.</summary>
        public virtual bool Expects(Exception exception) => false;
    }

    /// <summary>
    /// The text of a command in a report. The rule: the text of <c>ToString()</c> stays on one line. A backslash
    /// becomes <c>\\</c>; a line feed, a carriage return, and a tab become <c>\n</c>, <c>\r</c>, and <c>\t</c>; each
    /// other control, format, or line separator character becomes <c>\uXXXX</c>; and the marker <c>&lt;-</c>
    /// becomes <c>\u003c-</c>, so that only the report writes <c>&lt;- failed</c>. Ordinary text has no change. A
    /// <c>ToString()</c> that throws gives <c>&lt;ToString() threw TypeName&gt;</c>.
    /// </summary>
    internal static class CommandText
    {
        public static string Of(object? command)
        {
            string? text;
            try
            {
                text = command?.ToString();
            }
            catch (Exception ex)
            {
                return $"<ToString() threw {ex.GetType().Name}>";
            }

            return Escape(text ?? "null");
        }

        public static string Escape(string text)
        {
            StringBuilder? builder = null;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                string? escaped = c switch
                {
                    '\\' => "\\\\",
                    '\n' => "\\n",
                    '\r' => "\\r",
                    '\t' => "\\t",
                    '<' when i + 1 < text.Length && text[i + 1] == '-' => "\\u003c",
                    _ when NeedsEscape(c) => $"\\u{(int)c:x4}",
                    _ => null,
                };

                if (escaped == null)
                {
                    builder?.Append(c);
                    continue;
                }

                builder ??= new StringBuilder(text, 0, i, text.Length + 16);
                builder.Append(escaped);
            }

            return builder?.ToString() ?? text;
        }

        private static bool NeedsEscape(char c)
        {
            switch (char.GetUnicodeCategory(c))
            {
                case System.Globalization.UnicodeCategory.Control:
                case System.Globalization.UnicodeCategory.Format:
                case System.Globalization.UnicodeCategory.LineSeparator:
                case System.Globalization.UnicodeCategory.ParagraphSeparator:
                    return true;
                default:
                    return false;
            }
        }
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

        public override string ToString() => "[" + string.Join(", ", _commands.Select(CommandText.Of)) + "]";
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
                catch (Exception ex)
                {
                    // An expected exception goes on to T2 below, as a normal return does.
                    if (!IsExpected(command, ex))
                    {
                        // T3: an exception that the command does not expect.
                        throw new StatefulCommandFailedException(
                            $"Command {i + 1} threw an exception that it does not expect: {ex.GetType().Name}: {CommandText.Escape(ex.Message ?? string.Empty)}",
                            commands,
                            i,
                            ex);
                    }
                }

                // After a normal return or an expected exception: the model advances and the postcondition is checked.
                TModel next;
                bool holds;
                try
                {
                    next = command.NextModel(model);
                    holds = command.Postcondition(system, model, next);
                }
                catch (Exception ex)
                {
                    throw new StatefulCommandFailedException(
                        $"The model or the postcondition of command {i + 1} threw: {ex.GetType().Name}: {CommandText.Escape(ex.Message ?? string.Empty)}",
                        commands,
                        i,
                        ex);
                }

                if (!holds)
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

        /// <summary>An exception of <c>Expects</c> itself counts as not expected.</summary>
        private static bool IsExpected<TSystem, TModel>(Command<TSystem, TModel> command, Exception ex)
        {
            try
            {
                return command.Expects(ex);
            }
            catch
            {
                return false;
            }
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
                builder.AppendLine().Append(index + 1).Append(". ").Append(CommandText.Of(command));
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
