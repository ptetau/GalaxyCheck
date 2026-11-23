using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace GalaxyCheck.Runners
{
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "It reads the public members of a value to show it in a failure report. A value whose members trimming removed is shown by its ToString(), and its C# expression is marked as not available.")]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "It reads the public members of a value to show it in a failure report. A value whose members trimming removed is shown by its ToString(), and its C# expression is marked as not available.")]
    public static class ExampleRenderer
    {
        public static IEnumerable<string> Render(IReadOnlyList<object?> example) => example.Count switch
        {
            0 => RenderNullary(),
            1 => TryUnwrapTuple(example.Single(), out var unwrapped) ? RenderMultiary(unwrapped!) : RenderUnary(example),
            _ => RenderMultiary(example),
        };

        private static string[] RenderNullary()
        {
            return new string[] { "(no value)" };
        }

        private static string[] RenderUnary(IReadOnlyList<object?> example)
        {
            return new string[] { RenderValue(example.Single()) };
        }

        private static IEnumerable<string> RenderMultiary(IReadOnlyList<object?> example)
        {
            return example.Select((value, index) => $"[{index}] = {RenderValue(value)}");
        }

        private static bool TryUnwrapTuple(object? obj, out IReadOnlyList<object?>? unwrapped)
        {
            if (IsClassTupleInstance(obj))
            {
                unwrapped = obj!.GetType().GetProperties().Select(p => p.GetValue(obj)).ToList();
                return true;
            }
            else if (IsTupleLiteralInstance(obj))
            {
                unwrapped = obj!.GetType().GetFields().Where(f => f.Name != "Rest").Select(f => f.GetValue(obj)).ToList();
                return true;
            }

            unwrapped = null;
            return false;
        }

        private static string RenderValue(object? obj)
        {
            const int DepthLimit = 10;
            const int BreadthLimit = 10;

            var handler = new CompositeExampleRendererHandler(
                new List<IExampleRendererHandler>
                {
                    new AbbreviationRendererHandler(),
                    new StringExampleRendererHandler(),
                    new PrimitiveExampleRendererHandler(),
                    new StatefulCommandRendererHandler(),
                    new EnumerableExampleRendererHandler(elementLimit: BreadthLimit),
                    new ClassTupleRendererHandler(),
                    new TupleLiteralRendererHandler(),
                    new ObjectRendererHandler(propertyLimit: BreadthLimit)
                },
                DepthLimit);

            return handler.Render(obj, handler, ImmutableList.Create<object?>());
        }

        private interface IExampleRendererHandler
        {
            bool CanRender(object? obj);

            string Render(object? obj, IExampleRendererHandler renderer, ImmutableList<object?> path);
        }

        private class CompositeExampleRendererHandler : IExampleRendererHandler
        {
            private readonly IReadOnlyCollection<IExampleRendererHandler> _innerHandlers;
            private readonly int _depthLimit;

            public CompositeExampleRendererHandler(
                IReadOnlyCollection<IExampleRendererHandler> innerHandlers, int depthLimit)
            {
                _innerHandlers = innerHandlers;
                _depthLimit = depthLimit;
            }

            public bool CanRender(object? obj) => true;

            public string Render(object? obj, IExampleRendererHandler renderer, ImmutableList<object?> path)
            {
                if (path.Contains(obj))
                {
                    return "<Circular reference>";
                }

                if (path.Count >= _depthLimit)
                {
                    return "...";
                }

                try
                {
                    var innerHandler = _innerHandlers.FirstOrDefault(h => h.CanRender(obj));

                    if (innerHandler == null)
                    {
                        return obj?.ToString() ?? "null";
                    }

                    return innerHandler.Render(obj, renderer, path.Add(obj));
                }
                catch
                {
                    return "<Rendering failed>";
                }
            }
        }

        private class StringExampleRendererHandler : IExampleRendererHandler
        {
            public bool CanRender(object? obj) => obj is string;

            public string Render(object? obj, IExampleRendererHandler renderer, ImmutableList<object?> path) =>
                "\"" + (string)obj! + "\"";
        }

        private class PrimitiveExampleRendererHandler : IExampleRendererHandler
        {
            private static readonly ImmutableHashSet<Type> ExtraPrimitiveTypes =
                ImmutableHashSet.Create(typeof(string), typeof(decimal), typeof(DateTime));

            public bool CanRender(object? obj) =>
                obj is null ||
                obj.GetType().IsPrimitive ||
                obj.GetType().IsEnum ||
                ExtraPrimitiveTypes.Contains(obj.GetType()) ||
                obj is Delegate;

            public string Render(object? obj, IExampleRendererHandler renderer, ImmutableList<object?> path) => obj?.ToString() ?? "null";
        }

        /// <summary>Renders a stateful command by its own text, which holds its inputs, on one line.</summary>
        private class StatefulCommandRendererHandler : IExampleRendererHandler
        {
            public bool CanRender(object? obj) => obj is Stateful.IStatefulCommand;

            public string Render(object? obj, IExampleRendererHandler renderer, ImmutableList<object?> path) =>
                Stateful.CommandText.Of(obj);
        }

        private class EnumerableExampleRendererHandler : IExampleRendererHandler
        {
            private readonly int _elementLimit;

            public EnumerableExampleRendererHandler(int elementLimit)
            {
                _elementLimit = elementLimit;
            }

            public bool CanRender(object? obj) =>
                obj is not null &&
                obj.GetType().GetInterfaces().Any(t =>
                    t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            public string Render(object? obj, IExampleRendererHandler renderer, ImmutableList<object?> path)
            {
                var elements = ((IEnumerable)obj!).Cast<object>();

                var elementsToRender = elements.Take(_elementLimit).ToList();
                var hasMoreElements = elements.Skip(_elementLimit).Any();

                var renderedElements = Enumerable.Concat(
                    elementsToRender.Select(x => renderer.Render(x, renderer, path)),
                    hasMoreElements ? new[] { "..." } : Enumerable.Empty<string>());

                return "[" + string.Join(", ", renderedElements) + "]";
            }
        }

        private abstract class BaseTupleRendererHandler : IExampleRendererHandler
        {
            public abstract bool CanRender(object? obj);

            public string Render(object? obj, IExampleRendererHandler renderer, ImmutableList<object?> path)
            {
                var renderedElements = GetTupleElements(obj).Select((x) => $"{x.name} = {renderer.Render(x.value, renderer, path)}");

                return "(" + string.Join(", ", renderedElements) + ")";
            }

            protected abstract IEnumerable<(string name, object? value)> GetTupleElements(object? obj);
        }

        private class ClassTupleRendererHandler : BaseTupleRendererHandler
        {
            public override bool CanRender(object? obj) => IsClassTupleInstance(obj);

            protected override IEnumerable<(string name, object? value)> GetTupleElements(object? obj) =>
                obj!.GetType().GetProperties().Select(p => (p.Name, p.GetValue(obj!)));
        }

        private class TupleLiteralRendererHandler : BaseTupleRendererHandler
        {
            public override bool CanRender(object? obj) => IsTupleLiteralInstance(obj);

            protected override IEnumerable<(string name, object? value)> GetTupleElements(object? obj)
            {
                var fields = obj!.GetType().GetFields();

                var fieldsToRender = fields.Where(f => f.Name != "Rest").ToList();
                foreach (var field in fieldsToRender)
                {
                    yield return (field.Name, field.GetValue(obj));
                }

                var hasMoreFields = fields.Any(f => f.Name == "Rest");
                if (hasMoreFields)
                {
                    yield return ("Rest", AbbreviationRendererHandler.AbbreviationSymbol);
                }
            }
        }

        private class ObjectRendererHandler : IExampleRendererHandler
        {
            private readonly int _propertyLimit;

            public ObjectRendererHandler(int propertyLimit)
            {
                _propertyLimit = propertyLimit;
            }

            public bool CanRender(object? obj) =>
                obj is not null;

            public string Render(object? obj, IExampleRendererHandler renderer, ImmutableList<object?> path)
            {
                if (path.Where(x => x != obj).Select(x => x?.GetType()).Contains(obj!.GetType()))
                {
                    return "<Circular reference>";
                }

                var properties = obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
                if (properties.Length == 0)
                {
                    // Trimming can remove the members, and "{  }" shows nothing of the value.
                    return obj.ToString() ?? obj.GetType().Name;
                }

                var propertiesToRender = properties.Take(_propertyLimit).ToList();
                var hasMoreProperties = properties.Skip(_propertyLimit).Any();

                var renderedProperties = Enumerable.Concat(
                    propertiesToRender.Select(p => $"{p.Name} = {renderer.Render(p.GetValue(obj), renderer, path)}"),
                    hasMoreProperties ? new[] { "..." } : Enumerable.Empty<string>());

                return "{ " + string.Join(", ", renderedProperties) + " }";
            }
        }

        private class AbbreviationRendererHandler : IExampleRendererHandler
        {
            public static readonly object AbbreviationSymbol = new object();

            public bool CanRender(object? obj) => obj == AbbreviationSymbol;

            public string Render(object? obj, IExampleRendererHandler renderer, ImmutableList<object?> path) => "...";
        }

        /// <summary>
        /// True when the renderer shows the value by its ToString(), because it cannot read a public member of the
        /// value. Then the text is no C# expression.
        /// </summary>
        internal static bool HasNoReadableMembers(object? value)
        {
            if (value is null) return false;
            var type = value.GetType();
            return !type.IsPrimitive && !type.IsEnum && value is not string && value is not decimal && value is not DateTime &&
                   value is not Delegate && value is not IEnumerable && value is not Stateful.IStatefulCommand &&
                   type.GetInterface("System.Runtime.CompilerServices.ITuple") == null &&
                   type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Length == 0;
        }

        private static bool IsClassTupleInstance(object? value)
        {
            return
                value is not null &&
                value.GetType().GetInterface("System.Runtime.CompilerServices.ITuple") != null &&
                value!.GetType().GetFields().Any() == false;
        }

        private static bool IsTupleLiteralInstance(object? value)
        {
            return
                value is not null &&
                value.GetType().GetInterface("System.Runtime.CompilerServices.ITuple") != null &&
                value!.GetType().GetFields().Any() == true;
        }
    }
}
