using GalaxyCheck.Gens;
using GalaxyCheck.Runners;
using GalaxyCheck.Runners.Check;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace GalaxyCheck.Internal
{
    /// <summary>The fixed inputs of one [Example], and the property that runs the test function on them.</summary>
    internal sealed record PropertyExample(string Display, AsyncProperty Property);

    /// <summary>Thrown when an [Example] does not fit the parameters of its method.</summary>
    internal sealed class ExampleConfigurationException : Exception
    {
        public ExampleConfigurationException(string message) : base(message)
        {
        }
    }

    internal static class PropertyExamples
    {
        private static readonly MethodInfo ConstantMethod = typeof(Gen)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == nameof(Gen.Constant) && method.IsGenericMethodDefinition && method.GetParameters().Length == 1);

        private static readonly HealthCheck[] AllHealthChecks = Enum.GetValues<HealthCheck>();

        /// <returns>The examples of the method in order, or null when it has none.</returns>
        public static IReadOnlyList<PropertyExample>? Create(
            MethodInfo testMethodInfo,
            object? testClassInstance,
            object?[]? testMethodArguments,
            IPropertyFactory propertyFactory,
            IGenFactory? genFactory)
        {
            var attributes = testMethodInfo.GetCustomAttributes<ExampleAttribute>(inherit: true).ToList();
            if (attributes.Count == 0)
            {
                return null;
            }

            if (typeof(IGen).IsAssignableFrom(testMethodInfo.ReturnType))
            {
                throw new ExampleConfigurationException(
                    $"[Example] needs a method whose parameters are the inputs, but '{testMethodInfo.Name}' returns a property or a generator ({testMethodInfo.ReturnType.Name})." +
                    Environment.NewLine +
                    "Fix: remove the [Example] attributes, or write the property as a method that takes its inputs as parameters.");
            }

            // A token parameter is not an input, as it is not generated.
            var allParameters = testMethodInfo.GetParameters();
            var inputParameters = allParameters
                .Select((parameter, index) => (parameter, index))
                .Where(x => x.parameter.ParameterType != typeof(CancellationToken))
                .ToList();

            // A data row fixes the first parameters. An example on a row test gives only the inputs of the
            // parameters after the row values.
            var rowCount = Math.Min(testMethodArguments?.Length ?? 0, allParameters.Length);
            var freeParameters = inputParameters.Where(x => x.index >= rowCount).ToList();

            return attributes
                .Select(attribute =>
                {
                    var inputs = attribute.Inputs ?? new object?[] { null };
                    var display = Display(inputs);
                    var customGens = new Dictionary<int, IGen>();
                    for (var i = 0; i < rowCount; i++)
                    {
                        customGens[i] = Constant(allParameters[i].ParameterType, testMethodArguments![i]);
                    }

                    var parameters = freeParameters;
                    if (rowCount > 0 && inputs.Length != parameters.Count)
                    {
                        var testName = $"{testMethodInfo.DeclaringType?.Name}.{testMethodInfo.Name}";
                        throw new ExampleConfigurationException(
                            $"{display} on the test '{testName}' gives {Count(inputs.Length)}, but the test has a data row, so an example must give exactly {Count(parameters.Count)}: one for each parameter after the {Count(rowCount).Replace("input", "value")} of the data row." +
                            Environment.NewLine +
                            $"Fix: give {Count(parameters.Count)} in the [Example]{(parameters.Count == 0 ? "" : $", for {string.Join(", ", parameters.Select(x => $"'{x.parameter.Name}'"))} in order")}, and let the data row give the other values.");
                    }

                    for (var i = 0; i < parameters.Count; i++)
                    {
                        var (parameter, index) = parameters[i];
                        if (i >= inputs.Length)
                        {
                            throw new ExampleConfigurationException(
                                $"{display} gives {Count(inputs.Length)}, but the method has {parameters.Count} parameters{RowNote(rowCount)}: parameter '{parameter.Name}' ({TypeName(parameter.ParameterType)}) has no input." +
                                Environment.NewLine +
                                $"Fix: give one input for each parameter, in order, with an input for '{parameter.Name}'.");
                        }

                        if (!TryConvert(inputs[i], parameter.ParameterType, out var value))
                        {
                            throw new ExampleConfigurationException(
                                $"{display} gives the input {Format(inputs[i])} ({(inputs[i] == null ? "null" : TypeName(inputs[i]!.GetType()))}) for parameter '{parameter.Name}', which has the type {TypeName(parameter.ParameterType)}." +
                                Environment.NewLine +
                                $"Fix: give a value of type {TypeName(parameter.ParameterType)} for parameter '{parameter.Name}'.");
                        }

                        customGens[index] = Constant(parameter.ParameterType, value);
                    }

                    if (inputs.Length > parameters.Count)
                    {
                        throw new ExampleConfigurationException(
                            $"{display} gives {Count(inputs.Length)}, but the method has only {parameters.Count} parameters{RowNote(rowCount)}." +
                            Environment.NewLine +
                            "Fix: give one input for each parameter, in order.");
                    }

                    var property = propertyFactory.CreateProperty(testMethodInfo, testClassInstance, genFactory, customGens, testMethodArguments);
                    return new PropertyExample(display, property);
                })
                .ToList();
        }

        /// <summary>
        /// Runs the test function one time on the inputs of the example. A failing example is reported with its
        /// inputs, and is not shrunk.
        /// </summary>
        public static async Task Run(PropertyExample example, Func<string, string, string> formatMessage)
        {
            var result = await example.Property.CheckAsync(
                iterations: 1,
                seed: 0,
                size: 0,
                shrinkLimit: 0,
                deepCheck: false,
                exhaustive: false,
                suppressHealthChecks: AllHealthChecks);

            // An example is one input, so a cover rule, which judges the share of a label over a run, does not fail it.
            if (result.Counterexample != null)
            {
                var counterexample = result.Counterexample;
                throw new PropertyFailedException(
                    new Counterexample<object?>(
                        counterexample.Value,
                        counterexample.ReplayParameters,
                        counterexample.Replay,
                        counterexample.Exception,
                        counterexample.PresentedInput),
                    iterations: 1,
                    shrinks: 0,
                    formatReproduction: _ => $"{Environment.NewLine}    {example.Display}",
                    formatMessage: message => formatMessage(message, example.Display));
            }
        }

        private static IGen Constant(Type type, object? value) =>
            (IGen)ConstantMethod.MakeGenericMethod(type).Invoke(null, new[] { value })!;

        private static string RowNote(int rowCount) =>
            rowCount == 0 ? "" : $" after the {Count(rowCount).Replace("input", "value")} of the data row";

        internal static bool TryConvert(object? input, Type parameterType, out object? value)
        {
            value = input;
            var underlyingType = Nullable.GetUnderlyingType(parameterType) ?? parameterType;

            if (input == null)
            {
                return !parameterType.IsValueType || underlyingType != parameterType;
            }

            if (underlyingType.IsInstanceOfType(input))
            {
                return true;
            }

            if (underlyingType.IsEnum && IsInteger(input.GetType()))
            {
                value = Enum.ToObject(underlyingType, input);
                return true;
            }

            if (IsNumber(underlyingType) && IsNumber(input.GetType()))
            {
                try
                {
                    var converted = Convert.ChangeType(input, underlyingType, CultureInfo.InvariantCulture);
                    // Only a conversion that keeps the value, for example 1 to 1L, and not 1.5 to 2.
                    if (Convert.ChangeType(converted, input.GetType(), CultureInfo.InvariantCulture).Equals(input))
                    {
                        value = converted;
                        return true;
                    }
                }
                catch (Exception ex) when (ex is InvalidCastException || ex is OverflowException)
                {
                }
            }

            return false;
        }

        private static bool IsInteger(Type type) =>
            type == typeof(sbyte) || type == typeof(byte) || type == typeof(short) || type == typeof(ushort) ||
            type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong);

        private static bool IsNumber(Type type) =>
            IsInteger(type) || type == typeof(float) || type == typeof(double) || type == typeof(decimal);

        private static string Count(int inputs) => inputs == 1 ? "1 input" : $"{inputs} inputs";

        private static string Display(object?[] inputs) => $"[Example({string.Join(", ", inputs.Select(Format))})]";

        internal static string Format(object? value) =>
            CSharpExpression.TryFormat(value, out var expression) ? expression : value?.ToString() ?? "null";

        private static string TypeName(Type type) => type switch
        {
            _ when type == typeof(int) => "int",
            _ when type == typeof(long) => "long",
            _ when type == typeof(string) => "string",
            _ when type == typeof(bool) => "bool",
            _ when type == typeof(double) => "double",
            _ when type == typeof(object) => "object",
            _ when Nullable.GetUnderlyingType(type) is { } underlying => TypeName(underlying) + "?",
            _ => type.Name
        };
    }
}
