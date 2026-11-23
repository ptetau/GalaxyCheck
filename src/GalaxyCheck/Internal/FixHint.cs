using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GalaxyCheck.Internal
{
    /// <summary>
    /// The last line of a generator error: a line that starts with <c>Fix: </c> and gives the corrected call or the
    /// option to change.
    /// </summary>
    internal static class FixHint
    {
        public const string Prefix = "Fix: ";

        /// <summary>
        /// The fix line of a numeric generator whose minimum is above its maximum. Each numeric generator has
        /// GreaterThanEqual and LessThanEqual.
        /// </summary>
        public static readonly string NumericMinAboveMax = Environment.NewLine + Prefix +
            "give a minimum that is not above the maximum, for example .GreaterThanEqual(4).LessThanEqual(5), or swap the two bounds.";

        /// <summary>The fix line of Gen.Half() whose minimum is above its maximum. An int converts to Half only with a cast.</summary>
        public static readonly string HalfMinAboveMax = Environment.NewLine + Prefix +
            "give a minimum that is not above the maximum, for example .GreaterThanEqual((Half)4).LessThanEqual((Half)5), or swap the two bounds.";

        // A rule names only a method that each generator with the error has. A generator whose error needs a method of
        // its own type writes its fix line where it throws, and the rule here stays general.

        private static readonly IReadOnlyList<(Regex Pattern, Func<Match, string> Fix)> Rules = new (string, Func<Match, string>)[]
        {
            (@"'min' cannot be greater than 'max'",
                _ => "swap the two bounds of the generator, so that the minimum is not above the maximum."),
            (@"'origin' must be between 'min' and 'max'",
                _ => "give .ShrinkTowards(...) an origin between the minimum and the maximum, or widen the bounds."),
            (@"'(min|max)' must be a number",
                m => $"give a '{m.Groups[1].Value}' bound that is a number, not NaN."),
            (@"'(count|minCount|maxCount)' cannot be negative",
                m => $"give a '{m.Groups[1].Value}' of 0 or more, for example .WithCount(0) or .WithCountBetween(0, 10)."),
            (@"'minCount' cannot be greater than 'maxCount'",
                _ => "give a minimum count that is not above the maximum count, for example .WithCountBetween(0, 10)."),
            (@"'(length|minLength|maxLength)' cannot be negative",
                m => $"give a '{m.Groups[1].Value}' of 0 or more, for example .WithLengthBetween(0, 10)."),
            (@"'minLength' cannot be greater than 'maxLength'",
                _ => "give a minimum length that is not above the maximum length, for example .WithLengthBetween(0, 10)."),
            (@"(Count|Length) limit exceeded",
                m => $"give a {m.Groups[1].Value.ToLowerInvariant()} of at most 1000."),
            (@"'chars' must not be empty",
                _ => "give at least one char, for example .FromCharacters(\"abc\")."),
            (@"'charType' was not a valid flag value",
                _ => "give a value of Gen.CharType, for example Gen.CharType.Alphabetical."),
            (@"does not support the repeat range with a maximum below its minimum at position (\d+)",
                m => $"swap the two bounds of the repeat at position {m.Groups[1].Value}, so that the minimum comes first, for example a{{3,5}} in place of a{{5,3}}."),
            (@"does not support the range in reverse order at position (\d+)",
                m => $"swap the two ends of the character range at position {m.Groups[1].Value}, for example [a-z] in place of [z-a]."),
            (@"does not support the (unmatched '\)'|unclosed group|unclosed character class|trailing '\\'|short hexadecimal escape|invalid hexadecimal escape|quantifier without an element|group with no name|character class that matches no character) at position (\d+)",
                m => $"correct the {m.Groups[1].Value} at position {m.Groups[2].Value} of the pattern, so that the pattern is a valid regular expression."),
            (@"does not support the (.+?) at position (\d+)",
                m => $"remove the {m.Groups[1].Value} at position {m.Groups[2].Value} from the pattern, or generate with Gen.String() and filter with .Where(...)."),
            (@"'pattern' must not be null",
                _ => "give Gen.StringMatching(...) a pattern, for example Gen.StringMatching(\"[a-z]{3}\")."),
            (@"could not resolve type '([^']+)'",
                m => $"register a generator or a concrete type for '{ShortName(m.Groups[1].Value)}' on the factory that creates the value."),
            (@"candidate type '([^']+)' is not a concrete type",
                m => $"give RegisterImplementations only concrete types that derive from the registered type, not '{ShortName(m.Groups[1].Value)}'."),
            (@"the union '([^']+)' has no case",
                m => $"add a case to '{ShortName(m.Groups[1].Value)}', or register a generator for it with Gen.Factory().RegisterType(gen)."),
            (@"was not assignable to the type it was registered to",
                _ => "register a generator whose value type is the registered type, for example .RegisterType<T>(gen) with an IGen<T>."),
            (@"detected circular reference on type '([^']+)'",
                m => $"register a generator for '{ShortName(m.Groups[1].Value)}' with Gen.Factory().RegisterType(gen), or build it with Gen.Recursive(...)."),
            (@"parameter index '\d+' of custom generator was not valid",
                _ => "give a parameter index from 0 to the count of parameters minus 1."),
            (@"'(leafGen|wrap)' must not be null",
                m => $"give Gen.Recursive(...) a '{m.Groups[1].Value}' that is not null."),
            (@"'maxDepth' must be between 0 and (\d+)",
                m => $"give Gen.Recursive(...) a maxDepth from 0 to {m.Groups[1].Value}, for example maxDepth: 5."),
            (@"'from' datetime cannot be after 'until' datetime",
                _ => "give a 'from' value that is not after the 'until' value, or swap them."),
            (@"Type '([^']+)' is not an enum",
                m => $"give Gen.Enum<T>() an enum type, not '{ShortName(m.Groups[1].Value)}'."),
            (@"'source' must not be empty",
                _ => "give Gen.Element(...) at least one value."),
            (@"'choices' must not contain a negatively weighted generator",
                _ => "give each generator of Gen.Choose() a weight of 0 or more, for example .WithChoice(Gen.Int32(), 1)."),
            (@"'choices' must",
                _ => "give Gen.Choose() at least one generator with a weight above zero, for example .WithChoice(Gen.Int32(), 1)."),
            (@"'timeout' must be greater than zero",
                _ => "give a timeout above zero, for example timeout: TimeSpan.FromSeconds(1)."),
            (@"Error decoding replay string",
                _ => "copy the replay string exactly from the failure message, or remove the replay ([Replay] or replay:) to run new test cases."),
            (@"given replay string was no longer valid",
                _ => "remove the replay ([Replay] or replay:), run the property again, and copy the new replay string."),
            (@"Exception thrown\.",
                _ => "make the generator, or a function that it calls, not throw for the value in the exception above."),
        }.Select(rule => (new Regex(rule.Item1, RegexOptions.CultureInvariant), rule.Item2)).ToArray();

        /// <summary>Returns the message, with a fix line at the end when its last line is not one.</summary>
        public static string WithFix(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return Prefix + GenericFix;
            }

            var lastLine = message.Split('\n').Last().TrimStart();
            if (lastLine.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return message;
            }

            return message + Environment.NewLine + Prefix + For(message);
        }

        private const string GenericFix = "change the generator call or the option that the message names.";

        private static string For(string message)
        {
            foreach (var (pattern, fix) in Rules)
            {
                var match = pattern.Match(message);
                if (match.Success)
                {
                    return fix(match);
                }
            }

            return GenericFix;
        }

        /// <summary>The C# name of a type, for a C# example: <c>int</c>, <c>List&lt;int&gt;</c>, <c>int[]</c>, <c>int?</c>.</summary>
        public static string TypeName(Type type)
        {
            if (type.IsArray)
            {
                return $"{TypeName(type.GetElementType()!)}[{new string(',', type.GetArrayRank() - 1)}]";
            }

            if (Nullable.GetUnderlyingType(type) is { } underlying)
            {
                return TypeName(underlying) + "?";
            }

            if (Keywords.TryGetValue(type, out var keyword))
            {
                return keyword;
            }

            var name = type.Name;
            var tick = name.IndexOf('`');
            if (tick >= 0)
            {
                name = name.Substring(0, tick);
            }

            if (type.IsNested && type.DeclaringType != null && !type.IsGenericParameter)
            {
                name = $"{TypeName(ClosedDeclaringType(type))}.{name}";
            }

            // The type arguments of a nested type include those of its outer type; only the own arguments go here.
            var ownArguments = type.GetGenericArguments()
                .Skip(type.IsNested && type.DeclaringType != null ? type.DeclaringType.GetGenericArguments().Length : 0)
                .ToArray();
            return ownArguments.Length == 0 || tick < 0
                ? name
                : $"{name}<{string.Join(", ", ownArguments.Select(TypeName))}>";
        }

        /// <summary>
        /// The outer type of a nested type, with the type arguments of the nested type: reflection gives the open outer
        /// type, for example Outer&lt;T&gt; for Outer&lt;int&gt;.Inner.
        /// </summary>
        private static Type ClosedDeclaringType(Type type)
        {
            var outer = type.DeclaringType!;
            if (!outer.IsGenericTypeDefinition || type.IsGenericTypeDefinition || !type.IsGenericType)
            {
                return outer;
            }

            var count = outer.GetGenericArguments().Length;
            var arguments = type.GetGenericArguments();
            return arguments.Length < count ? outer : outer.MakeGenericType(arguments.Take(count).ToArray());
        }

        private static readonly Dictionary<Type, string> Keywords = new()
        {
            [typeof(bool)] = "bool", [typeof(byte)] = "byte", [typeof(sbyte)] = "sbyte", [typeof(char)] = "char",
            [typeof(short)] = "short", [typeof(ushort)] = "ushort", [typeof(int)] = "int", [typeof(uint)] = "uint",
            [typeof(long)] = "long", [typeof(ulong)] = "ulong", [typeof(float)] = "float", [typeof(double)] = "double",
            [typeof(decimal)] = "decimal", [typeof(string)] = "string", [typeof(object)] = "object",
            [typeof(nint)] = "nint", [typeof(nuint)] = "nuint",
        };

        /// <summary>The name of a type with no namespace, for a C# example.</summary>
        private static string ShortName(string fullName)
        {
            var name = fullName.Split('[')[0];
            var lastDot = name.LastIndexOfAny(new[] { '.', '+' });
            name = lastDot >= 0 ? name.Substring(lastDot + 1) : name;
            var tick = name.IndexOf('`');
            return tick >= 0 ? name.Substring(0, tick) : name;
        }
    }
}
