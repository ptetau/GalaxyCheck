using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace GalaxyCheck
{
    /// <summary>Formats a value as a C# expression.</summary>
    public static class CSharpExpression
    {
        private const int DepthLimit = 8;
        private const int ElementLimit = 200;

        /// <summary>
        /// Formats the value as a C# expression that makes an equal value. Returns false for a value with no
        /// expression. Each string and char is escaped, so the expression holds no code from a value.
        /// </summary>
        public static bool TryFormat(object? value, out string expression)
        {
            var builder = new StringBuilder();
            try
            {
                if (Write(builder, value, 0))
                {
                    expression = builder.ToString();
                    return true;
                }
            }
            catch (Exception)
            {
                // A value that throws when it is read has no expression.
            }

            expression = "";
            return false;
        }

        private static bool Write(StringBuilder b, object? value, int depth)
        {
            if (depth > DepthLimit) return false;

            switch (value)
            {
                case null:
                    b.Append("null");
                    return true;
                case bool v:
                    b.Append(v ? "true" : "false");
                    return true;
                case string v:
                    WriteString(b, v);
                    return true;
                case char v:
                    WriteChar(b, v);
                    return true;
                case int v:
                    if (v == int.MinValue) b.Append("int.MinValue");
                    else b.Append(v.ToString(CultureInfo.InvariantCulture));
                    return true;
                case long v:
                    if (v == long.MinValue) b.Append("long.MinValue");
                    else b.Append(v.ToString(CultureInfo.InvariantCulture)).Append('L');
                    return true;
                case uint v:
                    b.Append(v.ToString(CultureInfo.InvariantCulture)).Append('U');
                    return true;
                case ulong v:
                    b.Append(v.ToString(CultureInfo.InvariantCulture)).Append("UL");
                    return true;
                case short v:
                    b.Append("(short)(").Append(v.ToString(CultureInfo.InvariantCulture)).Append(')');
                    return true;
                case ushort v:
                    b.Append("(ushort)").Append(v.ToString(CultureInfo.InvariantCulture));
                    return true;
                case byte v:
                    b.Append("(byte)").Append(v.ToString(CultureInfo.InvariantCulture));
                    return true;
                case sbyte v:
                    b.Append("(sbyte)(").Append(v.ToString(CultureInfo.InvariantCulture)).Append(')');
                    return true;
                case double v:
                    WriteDouble(b, v);
                    return true;
                case float v:
                    WriteSingle(b, v);
                    return true;
                case decimal v:
                    b.Append(v.ToString(CultureInfo.InvariantCulture)).Append('m');
                    return true;
                case BigInteger v:
                    b.Append("global::System.Numerics.BigInteger.Parse(\"").Append(v.ToString(CultureInfo.InvariantCulture)).Append("\")");
                    return true;
                case Int128 v:
                    b.Append("global::System.Int128.Parse(\"").Append(v.ToString(CultureInfo.InvariantCulture)).Append("\")");
                    return true;
                case UInt128 v:
                    b.Append("global::System.UInt128.Parse(\"").Append(v.ToString(CultureInfo.InvariantCulture)).Append("\")");
                    return true;
                case Guid v:
                    b.Append("global::System.Guid.Parse(\"").Append(v.ToString("D")).Append("\")");
                    return true;
                case DateTime v:
                    b.Append("new global::System.DateTime(").Append(v.Ticks.ToString(CultureInfo.InvariantCulture))
                        .Append("L, global::System.DateTimeKind.").Append(v.Kind.ToString()).Append(')');
                    return true;
                case DateTimeOffset v:
                    b.Append("new global::System.DateTimeOffset(").Append(v.Ticks.ToString(CultureInfo.InvariantCulture))
                        .Append("L, global::System.TimeSpan.FromTicks(").Append(v.Offset.Ticks.ToString(CultureInfo.InvariantCulture)).Append("L))");
                    return true;
                case TimeSpan v:
                    b.Append("global::System.TimeSpan.FromTicks(").Append(v.Ticks.ToString(CultureInfo.InvariantCulture)).Append("L)");
                    return true;
                case DateOnly v:
                    b.Append("global::System.DateOnly.FromDayNumber(").Append(v.DayNumber.ToString(CultureInfo.InvariantCulture)).Append(')');
                    return true;
                case TimeOnly v:
                    b.Append("new global::System.TimeOnly(").Append(v.Ticks.ToString(CultureInfo.InvariantCulture)).Append("L)");
                    return true;
                case System.Text.Rune v:
                    b.Append("new global::System.Text.Rune(").Append(v.Value.ToString(CultureInfo.InvariantCulture)).Append(')');
                    return true;
                case Enum v:
                    return WriteEnum(b, v);
            }

            var type = value.GetType();

            if (type.IsArray)
            {
                return WriteArray(b, (Array)value, depth);
            }

            if (value is ITuple tuple && IsValueTuple(type))
            {
                b.Append('(');
                for (var i = 0; i < tuple.Length; i++)
                {
                    if (i > 0) b.Append(", ");
                    if (!Write(b, tuple[i], depth + 1)) return false;
                }

                b.Append(')');
                return true;
            }

            if (type.IsGenericType)
            {
                var definition = type.GetGenericTypeDefinition();
                if (definition == typeof(List<>) || definition == typeof(HashSet<>) || definition == typeof(SortedSet<>))
                {
                    return WriteCollection(b, type, (IEnumerable)value, depth);
                }

                if (definition == typeof(Dictionary<,>) || definition == typeof(SortedDictionary<,>))
                {
                    return WriteDictionary(b, type, (IDictionary)value, depth);
                }

                if (definition == typeof(KeyValuePair<,>))
                {
                    var (k, v) = (type.GetProperty("Key")!.GetValue(value), type.GetProperty("Value")!.GetValue(value));
                    b.Append("new ").Append(TypeName(type)).Append('(');
                    if (!Write(b, k, depth + 1)) return false;
                    b.Append(", ");
                    if (!Write(b, v, depth + 1)) return false;
                    b.Append(')');
                    return true;
                }
            }

            return WriteObject(b, value, type, depth);
        }

        private static bool IsValueTuple(Type type) =>
            type.IsGenericType && type.FullName != null && type.FullName.StartsWith("System.ValueTuple`", StringComparison.Ordinal);

        private static bool WriteEnum(StringBuilder b, Enum value)
        {
            var type = value.GetType();
            if (!IsNameable(type)) return false;

            var name = TypeName(type);
            if (Enum.IsDefined(type, value))
            {
                b.Append(name).Append('.').Append(Enum.GetName(type, value));
                return true;
            }

            var underlying = Convert.ToDecimal(Convert.ChangeType(value, Enum.GetUnderlyingType(type), CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
            b.Append('(').Append(name).Append(")(").Append(underlying.ToString(CultureInfo.InvariantCulture)).Append(')');
            return true;
        }

        private static bool WriteArray(StringBuilder b, Array array, int depth)
        {
            var elementType = array.GetType().GetElementType()!;
            if (array.Rank != 1 || !IsNameable(elementType) || array.Length > ElementLimit) return false;

            b.Append("new ").Append(TypeName(elementType)).Append("[] { ");
            var first = true;
            foreach (var item in array)
            {
                if (!first) b.Append(", ");
                first = false;
                if (!Write(b, item, depth + 1)) return false;
            }

            b.Append(first ? "}" : " }");
            return true;
        }

        private static bool WriteCollection(StringBuilder b, Type type, IEnumerable items, int depth)
        {
            if (!IsNameable(type)) return false;

            var list = items.Cast<object?>().ToList();
            if (list.Count > ElementLimit) return false;

            b.Append("new ").Append(TypeName(type)).Append(" { ");
            for (var i = 0; i < list.Count; i++)
            {
                if (i > 0) b.Append(", ");
                if (!Write(b, list[i], depth + 1)) return false;
            }

            b.Append(list.Count == 0 ? "}" : " }");

            return true;
        }

        private static bool WriteDictionary(StringBuilder b, Type type, IDictionary dictionary, int depth)
        {
            if (!IsNameable(type) || dictionary.Count > ElementLimit) return false;

            b.Append("new ").Append(TypeName(type)).Append(" { ");
            var first = true;
            foreach (DictionaryEntry entry in dictionary)
            {
                if (!first) b.Append(", ");
                first = false;
                b.Append('[');
                if (!Write(b, entry.Key, depth + 1)) return false;
                b.Append("] = ");
                if (!Write(b, entry.Value, depth + 1)) return false;
            }

            b.Append(first ? "}" : " }");
            return true;
        }

        /// <summary>
        /// Formats an object with a public constructor whose parameters are public members with the same names. The
        /// result must make an object with equal members, else the object has no expression.
        /// </summary>
        private static bool WriteObject(StringBuilder b, object value, Type type, int depth)
        {
            if (!IsNameable(type) || type.IsAbstract || typeof(Delegate).IsAssignableFrom(type) || type.IsPointer) return false;

            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .Where(c => !c.GetParameters().Any(p => p.ParameterType.IsByRef || p.ParameterType == type))
                .OrderByDescending(c => c.GetParameters().Length);

            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();
                var arguments = new object?[parameters.Length];
                var matched = true;
                for (var i = 0; i < parameters.Length; i++)
                {
                    if (!TryRead(value, type, parameters[i], out arguments[i]))
                    {
                        matched = false;
                        break;
                    }
                }

                if (!matched || !Rebuilds(constructor, arguments, value, type)) continue;

                var inner = new StringBuilder();
                inner.Append("new ").Append(TypeName(type)).Append('(');
                var ok = true;
                for (var i = 0; i < arguments.Length && ok; i++)
                {
                    if (i > 0) inner.Append(", ");
                    ok = Write(inner, arguments[i], depth + 1);
                }

                if (!ok) continue;

                inner.Append(')');
                b.Append(inner);
                return true;
            }

            return false;
        }

        private static bool TryRead(object value, Type type, ParameterInfo parameter, out object? result)
        {
            result = null;
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetIndexParameters().Length == 0 && property.CanRead &&
                    string.Equals(property.Name, parameter.Name, StringComparison.OrdinalIgnoreCase) &&
                    parameter.ParameterType.IsAssignableFrom(property.PropertyType))
                {
                    result = property.GetValue(value);
                    return true;
                }
            }

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (string.Equals(field.Name, parameter.Name, StringComparison.OrdinalIgnoreCase) &&
                    parameter.ParameterType.IsAssignableFrom(field.FieldType))
                {
                    result = field.GetValue(value);
                    return true;
                }
            }

            return false;
        }

        /// <summary>True when the constructor makes an object with the same public members as the value.</summary>
        private static bool Rebuilds(ConstructorInfo constructor, object?[] arguments, object value, Type type)
        {
            object rebuilt;
            try
            {
                rebuilt = constructor.Invoke((object?[])arguments.Clone());
            }
            catch (Exception)
            {
                return false;
            }

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetIndexParameters().Length != 0 || !property.CanRead) continue;
                if (!SameMember(property.GetValue(value), property.GetValue(rebuilt))) return false;
            }

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!SameMember(field.GetValue(value), field.GetValue(rebuilt))) return false;
            }

            return true;
        }

        private static bool SameMember(object? left, object? right)
        {
            if (Equals(left, right)) return true;
            if (left is IEnumerable l && right is IEnumerable r && left is not string)
            {
                return l.Cast<object?>().SequenceEqual(r.Cast<object?>());
            }

            return false;
        }

        /// <summary>True when the code can name the type: it is not compiler generated, and it is not a type parameter.</summary>
        private static bool IsNameable(Type type)
        {
            if (type.IsGenericParameter || type.IsPointer || type.IsByRef) return false;
            if (type.IsArray) return IsNameable(type.GetElementType()!);
            if (type.GetCustomAttribute<CompilerGeneratedAttribute>() != null) return false;
            var name = type.FullName ?? type.Name;
            if (name.IndexOfAny(new[] { '<', '>' }) >= 0) return false;
            return !type.IsGenericType || type.GetGenericArguments().All(IsNameable);
        }

        private static string TypeName(Type type)
        {
            if (type.IsArray) return TypeName(type.GetElementType()!) + "[]";

            if (type.IsGenericType)
            {
                var definition = type.GetGenericTypeDefinition();
                var name = definition.FullName!.Replace('+', '.');
                name = name.Substring(0, name.IndexOf('`'));
                return "global::" + name + "<" + string.Join(", ", type.GetGenericArguments().Select(TypeName)) + ">";
            }

            if (type == typeof(int)) return "int";
            if (type == typeof(long)) return "long";
            if (type == typeof(short)) return "short";
            if (type == typeof(byte)) return "byte";
            if (type == typeof(sbyte)) return "sbyte";
            if (type == typeof(uint)) return "uint";
            if (type == typeof(ulong)) return "ulong";
            if (type == typeof(ushort)) return "ushort";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(string)) return "string";
            if (type == typeof(char)) return "char";
            if (type == typeof(double)) return "double";
            if (type == typeof(float)) return "float";
            if (type == typeof(decimal)) return "decimal";
            if (type == typeof(object)) return "object";

            return "global::" + (type.FullName ?? type.Name).Replace('+', '.');
        }

        private static void WriteDouble(StringBuilder b, double v)
        {
            if (double.IsNaN(v)) b.Append("double.NaN");
            else if (double.IsPositiveInfinity(v)) b.Append("double.PositiveInfinity");
            else if (double.IsNegativeInfinity(v)) b.Append("double.NegativeInfinity");
            else if (v == 0 && double.IsNegative(v)) b.Append("-0.0");
            else
            {
                var text = v.ToString("R", CultureInfo.InvariantCulture);
                b.Append(text);
                if (text.IndexOfAny(new[] { '.', 'E', 'e' }) < 0) b.Append(".0");
            }
        }

        private static void WriteSingle(StringBuilder b, float v)
        {
            if (float.IsNaN(v)) b.Append("float.NaN");
            else if (float.IsPositiveInfinity(v)) b.Append("float.PositiveInfinity");
            else if (float.IsNegativeInfinity(v)) b.Append("float.NegativeInfinity");
            else if (v == 0 && float.IsNegative(v)) b.Append("-0.0f");
            else b.Append(v.ToString("R", CultureInfo.InvariantCulture)).Append('f');
        }

        private static void WriteString(StringBuilder b, string value)
        {
            b.Append('"');
            foreach (var c in value) AppendEscaped(b, c, '"');
            b.Append('"');
        }

        private static void WriteChar(StringBuilder b, char value)
        {
            b.Append('\'');
            AppendEscaped(b, value, '\'');
            b.Append('\'');
        }

        /// <summary>
        /// Appends the char so that it cannot end the literal or start code: only printable ASCII goes out as it is.
        /// </summary>
        private static void AppendEscaped(StringBuilder b, char c, char quote)
        {
            switch (c)
            {
                case '\\': b.Append("\\\\"); return;
                case '"': b.Append(quote == '"' ? "\\\"" : "\""); return;
                case '\'': b.Append(quote == '\'' ? "\\'" : "'"); return;
                case '\0': b.Append("\\0"); return;
                case '\a': b.Append("\\a"); return;
                case '\b': b.Append("\\b"); return;
                case '\f': b.Append("\\f"); return;
                case '\n': b.Append("\\n"); return;
                case '\r': b.Append("\\r"); return;
                case '\t': b.Append("\\t"); return;
                case '\v': b.Append("\\v"); return;
            }

            if (c >= ' ' && c <= '~')
            {
                b.Append(c);
            }
            else
            {
                b.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
            }
        }
    }
}
