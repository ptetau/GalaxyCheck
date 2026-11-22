namespace GalaxyCheck
{
    using GalaxyCheck.Gens;

    public static partial class Gen
    {
        /// <summary>
        /// Generates strings that match the regular expression. The generator supports literals, escapes such as
        /// \d \w \s, character classes, alternation, groups, the quantifiers ? * + {n} {n,} {n,m}, and anchors at the
        /// ends. A construct that it does not support, such as a lookbehind, gives a generator error.
        /// </summary>
        /// <param name="pattern">The regular expression that each value matches.</param>
        /// <returns>The new generator.</returns>
        public static IGen<string> StringMatching(string pattern)
        {
            if (pattern is null)
            {
                return Advanced.Error<string>("'pattern' must not be null");
            }

            var (node, error) = PatternParser.Parse(pattern);
            return node is null
                ? Advanced.Error<string>(error!)
                : new PatternGen(node);
        }
    }
}

namespace GalaxyCheck.Gens
{
    using GalaxyCheck.Gens.Internal;
    using GalaxyCheck.Gens.Parameters;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// Generates strings from a parsed pattern. It never runs a match of the pattern: it builds each string from the
    /// choices of the source, so a failing value shrinks to fewer repeats, earlier alternatives, and earlier
    /// characters of each class.
    /// </summary>
    internal sealed record PatternGen(PatternNode Node) : BaseGen<string>
    {
        public override string Generate(GenSource source)
        {
            var builder = new StringBuilder();
            Node.Append(source, builder);
            return builder.ToString();
        }
    }

    internal abstract class PatternNode
    {
        public abstract void Append(GenSource source, StringBuilder builder);

        /// <summary>The maximum length of a string that the node makes.</summary>
        public abstract long MaxLength { get; }
    }

    internal sealed class PatternEmpty : PatternNode
    {
        public static readonly PatternEmpty Instance = new PatternEmpty();

        public override void Append(GenSource source, StringBuilder builder)
        {
        }

        public override long MaxLength => 0;
    }

    internal sealed class PatternLiteral : PatternNode
    {
        private readonly char _char;

        public PatternLiteral(char c)
        {
            _char = c;
        }

        public override void Append(GenSource source, StringBuilder builder) => builder.Append(_char);

        public override long MaxLength => 1;
    }

    internal sealed class PatternCharClass : PatternNode
    {
        private readonly char[] _chars;

        public PatternCharClass(char[] chars)
        {
            _chars = chars;
        }

        public override void Append(GenSource source, StringBuilder builder) =>
            builder.Append(_chars[IntCore.Index(source, _chars.Length - 1, Gen.Bias.WithSize)]);

        public override long MaxLength => 1;
    }

    internal sealed class PatternSequence : PatternNode
    {
        private readonly PatternNode[] _nodes;

        public PatternSequence(PatternNode[] nodes)
        {
            _nodes = nodes;
        }

        public override void Append(GenSource source, StringBuilder builder)
        {
            foreach (var node in _nodes)
            {
                node.Append(source, builder);
            }
        }

        public override long MaxLength
        {
            get
            {
                long total = 0;
                foreach (var node in _nodes)
                {
                    total = Math.Min(total + node.MaxLength, long.MaxValue / 2);
                }

                return total;
            }
        }
    }

    internal sealed class PatternAlternation : PatternNode
    {
        private readonly PatternNode[] _alternatives;

        public PatternAlternation(PatternNode[] alternatives)
        {
            _alternatives = alternatives;
        }

        public override void Append(GenSource source, StringBuilder builder) =>
            _alternatives[IntCore.Index(source, _alternatives.Length - 1, Gen.Bias.WithSize)].Append(source, builder);

        public override long MaxLength
        {
            get
            {
                long max = 0;
                foreach (var alternative in _alternatives)
                {
                    max = Math.Max(max, alternative.MaxLength);
                }

                return max;
            }
        }
    }

    /// <summary>
    /// Repeats a node. Before each repeat above the minimum, a choice of 1 means "one more" and 0 means "stop", so
    /// that the shrinker removes a repeat by deleting its span, as for a list.
    /// </summary>
    internal sealed class PatternRepeat : PatternNode
    {
        private readonly PatternNode _node;
        private readonly int _min;
        private readonly int _max;

        public PatternRepeat(PatternNode node, int min, int max)
        {
            _node = node;
            _min = min;
            _max = max;
        }

        public override void Append(GenSource source, StringBuilder builder)
        {
            if (!source.IsReplaying)
            {
                var count = IntCore.RandomInt32(source, _min, _max, Gen.Bias.WithSize);
                for (var i = 0; i < count; i++)
                {
                    var start = source.BeginSpan();
                    if (i >= _min)
                    {
                        source.Record(1);
                    }

                    _node.Append(source, builder);
                    source.EndSpan(start);
                }

                if (count < _max)
                {
                    source.Record(0);
                }
            }
            else
            {
                for (var i = 0; i < _max; i++)
                {
                    var start = source.BeginSpan();
                    if (i >= _min && source.Read(1) == 0)
                    {
                        break;
                    }

                    _node.Append(source, builder);
                    source.EndSpan(start);
                }
            }
        }

        public override long MaxLength => Math.Min(_node.MaxLength * _max, long.MaxValue / 2);
    }

    /// <summary>
    /// Parses the common constructs of a regular expression. A construct with no support gives an error that names
    /// it.
    /// </summary>
    internal sealed class PatternParser
    {
        /// <summary>The number of extra repeats that an unbounded quantifier (*, +, {n,}) allows.</summary>
        public const int UnboundedRepeatLimit = 20;

        public const int MaxPatternLength = 10_000;

        public const int MaxRepeat = 1_000;

        public const long MaxStringLength = 100_000;

        private const int MaxNesting = 100;

        // Negated classes and the dot draw from the printable ASCII characters.
        private static readonly (int, int)[] Universe = { (0x20, 0x7E) };
        private static readonly (int, int)[] Digits = { ('0', '9') };
        private static readonly (int, int)[] WordChars = { ('0', '9'), ('A', 'Z'), ('_', '_'), ('a', 'z') };
        private static readonly (int, int)[] SpaceChars = { ('\t', '\r'), (' ', ' ') };

        private readonly string _pattern;
        private int _pos;
        private int _nesting;

        private PatternParser(string pattern)
        {
            _pattern = pattern;
        }

        public static (PatternNode? Node, string? Error) Parse(string pattern)
        {
            if (pattern.Length > MaxPatternLength)
            {
                return (null, $"the pattern is longer than {MaxPatternLength} characters");
            }

            try
            {
                var parser = new PatternParser(pattern);
                var node = parser.ParseAlternation();
                if (parser._pos < pattern.Length)
                {
                    // Only an unmatched ')' stops the top-level parse early.
                    throw Unsupported("unmatched ')'", parser._pos);
                }

                if (node.MaxLength > MaxStringLength)
                {
                    return (null, $"the pattern can make strings longer than {MaxStringLength} characters");
                }

                return (node, null);
            }
            catch (PatternException ex)
            {
                return (null, ex.Message);
            }
        }

        private sealed class PatternException : Exception
        {
            public PatternException(string message) : base(message)
            {
            }
        }

        private static PatternException Unsupported(string construct, int position) =>
            new PatternException($"Gen.StringMatching does not support the {construct} at position {position} of the pattern");

        private bool AtEnd => _pos >= _pattern.Length;

        private char Peek => _pattern[_pos];

        private bool LookingAt(string text) =>
            string.CompareOrdinal(_pattern, _pos, text, 0, text.Length) == 0;

        private PatternNode ParseAlternation()
        {
            if (++_nesting > MaxNesting)
            {
                throw Unsupported($"nesting deeper than {MaxNesting} groups", _pos);
            }

            var alternatives = new List<PatternNode> { ParseSequence() };
            while (!AtEnd && Peek == '|')
            {
                _pos++;
                alternatives.Add(ParseSequence());
            }

            _nesting--;
            return alternatives.Count == 1 ? alternatives[0] : new PatternAlternation(alternatives.ToArray());
        }

        private PatternNode ParseSequence()
        {
            var nodes = new List<PatternNode>();
            while (!AtEnd && Peek != '|' && Peek != ')')
            {
                var atomStart = _pos;
                var atom = ParseAtom();
                nodes.Add(ParseQuantifiers(atom, atomStart));
            }

            return nodes.Count switch
            {
                0 => PatternEmpty.Instance,
                1 => nodes[0],
                _ => new PatternSequence(nodes.ToArray())
            };
        }

        private PatternNode ParseQuantifiers(PatternNode atom, int atomStart)
        {
            var node = atom;
            var quantified = false;
            while (!AtEnd)
            {
                var start = _pos;
                int min, max;
                switch (Peek)
                {
                    case '?': _pos++; (min, max) = (0, 1); break;
                    case '*': _pos++; (min, max) = (0, UnboundedRepeatLimit); break;
                    case '+': _pos++; (min, max) = (1, 1 + UnboundedRepeatLimit); break;
                    case '{':
                        if (!TryParseBraces(out min, out max))
                        {
                            return node;
                        }

                        break;
                    default:
                        return node;
                }

                if (quantified)
                {
                    throw Unsupported("nested quantifier", start);
                }

                if (node is PatternAnchor)
                {
                    throw Unsupported("quantifier on an anchor", start);
                }

                quantified = true;
                node = new PatternRepeat(node, min, max);

                // A lazy quantifier makes the same strings.
                if (!AtEnd && Peek == '?')
                {
                    _pos++;
                }
            }

            return node;
        }

        private bool TryParseBraces(out int min, out int max)
        {
            min = max = 0;
            var end = _pattern.IndexOf('}', _pos);
            if (end < 0)
            {
                return false;
            }

            var body = _pattern.Substring(_pos + 1, end - _pos - 1);
            var comma = body.IndexOf(',');
            var minText = comma < 0 ? body : body.Substring(0, comma);
            if (!TryParseCount(minText, out min))
            {
                return false;
            }

            if (comma < 0)
            {
                max = min;
            }
            else
            {
                var maxText = body.Substring(comma + 1);
                if (maxText.Length == 0)
                {
                    max = min + UnboundedRepeatLimit;
                }
                else if (!TryParseCount(maxText, out max))
                {
                    return false;
                }
            }

            if (min > MaxRepeat || max > MaxRepeat + UnboundedRepeatLimit)
            {
                throw Unsupported($"repeat count above {MaxRepeat}", _pos);
            }

            if (max < min)
            {
                throw Unsupported("repeat range with a maximum below its minimum", _pos);
            }

            _pos = end + 1;
            return true;
        }

        private static bool TryParseCount(string text, out int value)
        {
            value = 0;
            if (text.Length == 0 || !IsDigits(text))
            {
                return false;
            }

            // A count too long for an int is above the repeat limit, which the caller reports.
            value = text.Length > 9 ? int.MaxValue : int.Parse(text, NumberStyles.None, CultureInfo.InvariantCulture);
            return true;
        }

        private static bool IsDigits(string text)
        {
            foreach (var c in text)
            {
                if (c < '0' || c > '9')
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Parses one atom. An anchor at an end of the pattern makes nothing.</summary>
        private PatternNode ParseAtom()
        {
            var start = _pos;
            var c = Peek;
            switch (c)
            {
                case '(':
                    return ParseGroup();
                case '[':
                    return new PatternCharClass(ToChars(ParseClass(), start));
                case '.':
                    _pos++;
                    return new PatternCharClass(ToChars(Universe, start));
                case '^':
                    _pos++;
                    return Anchor(start == 0, "^", start);
                case '$':
                    _pos++;
                    return Anchor(_pos == _pattern.Length, "$", start);
                case '*':
                case '+':
                case '?':
                    throw Unsupported("quantifier without an element", start);
                case '{':
                    if (TryParseBraces(out _, out _))
                    {
                        throw Unsupported("quantifier without an element", start);
                    }

                    _pos++;
                    return new PatternLiteral('{');
                case '\\':
                    return ParseEscapeAtom();
                default:
                    _pos++;
                    return new PatternLiteral(c);
            }
        }

        private PatternNode Anchor(bool atEnd, string name, int position)
        {
            if (!atEnd)
            {
                throw Unsupported($"anchor '{name}' in the middle of the pattern", position);
            }

            return PatternAnchor.Instance;
        }

        private PatternNode ParseGroup()
        {
            var start = _pos;
            _pos++;
            if (!AtEnd && Peek == '?')
            {
                if (LookingAt("?:"))
                {
                    _pos += 2;
                }
                else if (LookingAt("?<=") || LookingAt("?<!"))
                {
                    throw Unsupported("lookbehind", start);
                }
                else if (LookingAt("?=") || LookingAt("?!"))
                {
                    throw Unsupported("lookahead", start);
                }
                else if (LookingAt("?>"))
                {
                    throw Unsupported("atomic group", start);
                }
                else if (LookingAt("?("))
                {
                    throw Unsupported("conditional", start);
                }
                else if (LookingAt("?#"))
                {
                    throw Unsupported("comment", start);
                }
                else if (LookingAt("?<") || LookingAt("?'"))
                {
                    // A named group generates as its contents.
                    var close = LookingAt("?'") ? '\'' : '>';
                    var end = _pattern.IndexOf(close, _pos + 2);
                    var name = end < 0 ? "" : _pattern.Substring(_pos + 2, end - _pos - 2);
                    if (name.Contains('-'))
                    {
                        throw Unsupported("balancing group", start);
                    }

                    if (end < 0 || name.Length == 0)
                    {
                        throw Unsupported("group with no name", start);
                    }

                    _pos = end + 1;
                }
                else
                {
                    throw Unsupported("inline option", start);
                }
            }

            var inner = ParseAlternation();
            if (AtEnd || Peek != ')')
            {
                throw Unsupported("unclosed group", start);
            }

            _pos++;
            return inner;
        }

        private PatternNode ParseEscapeAtom()
        {
            var start = _pos;
            if (_pos + 1 >= _pattern.Length)
            {
                throw Unsupported("trailing '\\'", start);
            }

            var c = _pattern[_pos + 1];
            switch (c)
            {
                case 'A':
                    _pos += 2;
                    return Anchor(start == 0, "\\A", start);
                case 'z':
                case 'Z':
                    _pos += 2;
                    return Anchor(_pos == _pattern.Length, "\\" + c, start);
                case 'b':
                case 'B':
                    throw Unsupported("word boundary", start);
                case 'G':
                    throw Unsupported("anchor '\\G'", start);
                case 'k':
                    throw Unsupported("backreference", start);
                default:
                    if (c >= '1' && c <= '9')
                    {
                        throw Unsupported("backreference", start);
                    }

                    var set = ParseEscape(inClass: false);
                    return new PatternCharClass(ToChars(set, start));
            }
        }

        /// <summary>Parses an escape at the current '\' and returns its set of characters.</summary>
        private (int, int)[] ParseEscape(bool inClass)
        {
            var start = _pos;
            _pos++;
            if (AtEnd)
            {
                throw Unsupported("trailing '\\'", start);
            }

            var c = Peek;
            _pos++;
            switch (c)
            {
                case 'd': return Digits;
                case 'D': return Subtract(Universe, Digits);
                case 'w': return WordChars;
                case 'W': return Subtract(Universe, WordChars);
                case 's': return SpaceChars;
                case 'S': return Subtract(Universe, SpaceChars);
                case 't': return Single('\t');
                case 'n': return Single('\n');
                case 'r': return Single('\r');
                case 'f': return Single('\f');
                case 'v': return Single('\v');
                case 'a': return Single('\a');
                case 'e': return Single('\u001B');
                case 'b' when inClass: return Single('\b');
                case '0': return Single('\0');
                case 'x': return Single(ParseHex(2, start));
                case 'u': return Single(ParseHex(4, start));
                case 'p':
                case 'P':
                    throw Unsupported("unicode category", start);
                case 'c':
                    throw Unsupported("control character escape", start);
                default:
                    if (char.IsLetterOrDigit(c))
                    {
                        throw Unsupported($"escape '\\{c}'", start);
                    }

                    return Single(c);
            }
        }

        private char ParseHex(int digits, int start)
        {
            if (_pos + digits > _pattern.Length)
            {
                throw Unsupported("short hexadecimal escape", start);
            }

            var text = _pattern.Substring(_pos, digits);
            if (!int.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var value))
            {
                throw Unsupported("invalid hexadecimal escape", start);
            }

            _pos += digits;
            return (char)value;
        }

        private (int, int)[] ParseClass()
        {
            var start = _pos;
            _pos++;
            var negated = false;
            if (!AtEnd && Peek == '^')
            {
                negated = true;
                _pos++;
            }

            var ranges = new List<(int, int)>();
            var first = true;
            while (true)
            {
                if (AtEnd)
                {
                    throw Unsupported("unclosed character class", start);
                }

                if (Peek == ']' && !first)
                {
                    _pos++;
                    break;
                }

                first = false;

                if (LookingAt("-["))
                {
                    throw Unsupported("character class subtraction", _pos);
                }

                if (LookingAt("[:"))
                {
                    throw Unsupported("POSIX character class", _pos);
                }

                var itemStart = _pos;
                var (set, single) = ParseClassItem();
                if (single is char low && !AtEnd && Peek == '-' && _pos + 1 < _pattern.Length && _pattern[_pos + 1] != ']')
                {
                    _pos++;
                    if (LookingAt("["))
                    {
                        throw Unsupported("character class subtraction", _pos - 1);
                    }

                    var (_, highSingle) = ParseClassItem();
                    if (highSingle is not char high)
                    {
                        throw Unsupported("range with a class escape", itemStart);
                    }

                    if (high < low)
                    {
                        throw Unsupported("range in reverse order", itemStart);
                    }

                    ranges.Add((low, high));
                }
                else
                {
                    ranges.AddRange(set);
                }
            }

            var normalized = Normalize(ranges);
            return negated ? Subtract(Universe, normalized) : normalized;
        }

        private ((int, int)[] Set, char? Single) ParseClassItem()
        {
            if (Peek == '\\')
            {
                var set = ParseEscape(inClass: true);
                return (set, set.Length == 1 && set[0].Item1 == set[0].Item2 ? (char)set[0].Item1 : null);
            }

            var c = Peek;
            _pos++;
            return (Single(c), c);
        }

        private static (int, int)[] Single(char c) => new[] { ((int)c, (int)c) };

        private static (int, int)[] Normalize(List<(int, int)> ranges)
        {
            ranges.Sort((a, b) => a.Item1.CompareTo(b.Item1));
            var result = new List<(int, int)>();
            foreach (var (lo, hi) in ranges)
            {
                if (result.Count > 0 && lo <= result[^1].Item2 + 1)
                {
                    var last = result[^1];
                    result[^1] = (last.Item1, Math.Max(last.Item2, hi));
                }
                else
                {
                    result.Add((lo, hi));
                }
            }

            return result.ToArray();
        }

        private static (int, int)[] Subtract((int, int)[] from, (int, int)[] remove)
        {
            var result = new List<(int, int)>();
            foreach (var (lo, hi) in from)
            {
                var current = lo;
                foreach (var (rlo, rhi) in remove)
                {
                    if (rhi < current || rlo > hi)
                    {
                        continue;
                    }

                    if (rlo > current)
                    {
                        result.Add((current, rlo - 1));
                    }

                    current = Math.Max(current, rhi + 1);
                }

                if (current <= hi)
                {
                    result.Add((current, hi));
                }
            }

            return result.ToArray();
        }

        private static char[] ToChars((int, int)[] ranges, int position)
        {
            var count = 0;
            foreach (var (lo, hi) in ranges)
            {
                count += hi - lo + 1;
            }

            if (count == 0)
            {
                throw Unsupported("character class that matches no character", position);
            }

            var chars = new char[count];
            var i = 0;
            foreach (var (lo, hi) in ranges)
            {
                for (var c = lo; c <= hi; c++)
                {
                    chars[i++] = (char)c;
                }
            }

            return chars;
        }
    }

    /// <summary>An anchor at an end of the pattern. It makes no character.</summary>
    internal sealed class PatternAnchor : PatternNode
    {
        public static readonly PatternAnchor Instance = new PatternAnchor();

        public override void Append(GenSource source, StringBuilder builder)
        {
        }

        public override long MaxLength => 0;
    }
}
