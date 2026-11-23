#pragma warning disable IDE1006 // Naming Styles
using GalaxyCheck.Properties;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GalaxyCheck
{
    public partial class Property
    {
        /// <summary>The input of one test case.</summary>
        public interface TestInput<out TInput>
        {
            /// <summary>The generated input.</summary>
            TInput Input { get; }

            /// <summary>The values of the input as a failure report shows them, one for each parameter.</summary>
            IReadOnlyList<object?> PresentedInput { get; }
        }

        /// <summary>One test case of a property: its input and its lazy outcome.</summary>
        public interface Test<out T> : TestInput<T>
        {
            /// <summary>The outcome of the test function, computed on first use.</summary>
            Lazy<TestOutput> Output { get; }
        }

        /// <summary>One test case of a property with a boxed input.</summary>
        public interface Test : Test<object?>
        {
        }

        /// <summary>One test case of an async property: its input and its lazy outcome.</summary>
        public interface AsyncTest<out T> : TestInput<T>
        {
            /// <summary>The outcome of the async test function, computed on first use.</summary>
            Lazy<ValueTask<TestOutput>> Output { get; }
        }

        /// <summary>One test case of an async property with a boxed input.</summary>
        public interface AsyncTest : AsyncTest<object?>
        {
        }
    }

    public static partial class Extensions
    {
        /// <summary>Returns the test case with its input cast to <typeparamref name="T"/>.</summary>
        public static Property.Test<T> Cast<T>(this Property.Test test) =>
            TestFactory.Create((T)test.Input!, test.Output, test.PresentedInput);

        /// <summary>Returns the async test case with its input cast to <typeparamref name="T"/>.</summary>
        public static Property.AsyncTest<T> Cast<T>(this Property.AsyncTest test) =>
            TestFactory.Create((T)test.Input!, test.Output, test.PresentedInput);

        /// <summary>Returns the test case as an async test case.</summary>
        public static Property.AsyncTest<T> AsAsync<T>(this Property.Test<T> test) => TestFactory.Create<T>(
            test.Input,
            new Lazy<ValueTask<Property.TestOutput>>(() => ValueTask.FromResult(test.Output.Value)),
            test.PresentedInput);
    }
}

#pragma warning disable IDE1006 // Naming Styles
