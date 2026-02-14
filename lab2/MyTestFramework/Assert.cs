using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MyTestFramework.Exceptions;

namespace MyTestFramework
{
    public static class Assert
    {

        public static void IsTrue(bool condition, string message = null)
        {
            if (!condition)
            {
                throw new TestAssertionException(message ?? "Expected condition to be true, but was false");
            }
        }

        public static void IsFalse(bool condition, string message = null)
        {
            if (condition)
            {
                throw new TestAssertionException(message ?? "Expected condition to be false, but was true");
            }
        }

        public static void AreEqual(object expected, object actual, string message = null)
        {
            if (!Equals(expected, actual))
            {
                throw new TestAssertionException(
                    message ?? $"Expected {FormatValue(expected)}, but was {FormatValue(actual)}");
            }
        }

        public static void AreNotEqual(object expected, object actual, string message = null)
        {
            if (Equals(expected, actual))
            {
                throw new TestAssertionException(
                    message ?? $"Expected values to be different, but both were {FormatValue(expected)}");
            }
        }

        public static void IsNull(object obj, string message = null)
        {
            if (obj != null)
            {
                throw new TestAssertionException(message ?? $"Expected null, but was {FormatValue(obj)}");
            }
        }

        public static void IsNotNull(object obj, string message = null)
        {
            if (obj == null)
            {
                throw new TestAssertionException(message ?? "Expected not null, but was null");
            }
        }

        public static void Contains(string substring, string actualString, string message = null)
        {
            IsNotNull(actualString, "Actual string cannot be null");
            IsNotNull(substring, "Substring cannot be null");

            if (!actualString.Contains(substring))
            {
                throw new TestAssertionException(
                    message ?? $"Expected string '{actualString}' to contain '{substring}'");
            }
        }

        public static void StartsWith(string prefix, string actualString, string message = null)
        {
            IsNotNull(actualString, "Actual string cannot be null");
            IsNotNull(prefix, "Prefix cannot be null");

            if (!actualString.StartsWith(prefix))
            {
                throw new TestAssertionException(
                    message ?? $"Expected string '{actualString}' to start with '{prefix}'");
            }
        }

        public static void IsEmpty(string actualString, string message = null)
        {
            if (actualString == null || actualString.Length != 0)
            {
                throw new TestAssertionException(
                    message ?? $"Expected empty string, but was '{actualString}'");
            }
        }

        public static void Contains<T>(T element, IEnumerable<T> collection, string message = null)
        {
            IsNotNull(collection, "Collection cannot be null");

            if (!collection.Contains(element))
            {
                throw new TestAssertionException(
                    message ?? $"Expected collection to contain {FormatValue(element)}");
            }
        }

        public static void IsEmpty(IEnumerable collection, string message = null)
        {
            IsNotNull(collection, "Collection cannot be null");

            var enumerator = collection.GetEnumerator();
            if (enumerator.MoveNext())
            {
                throw new TestAssertionException(message ?? "Expected empty collection, but it contains elements");
            }
        }

        public static void AllItemsAreNotNull(IEnumerable collection, string message = null)
        {
            IsNotNull(collection, "Collection cannot be null");

            foreach (var item in collection)
            {
                if (item == null)
                {
                    throw new TestAssertionException(message ?? "Expected all items to be not null, but found null item");
                }
            }
        }

        public static TException Throws<TException>(Action action, string message = null) where TException : Exception
        {
            IsNotNull(action, "Action cannot be null");

            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }
            catch (Exception ex)
            {
                throw new TestAssertionException(
                    message ?? $"Expected exception of type {typeof(TException).Name}, but got {ex.GetType().Name}: {ex.Message}");
            }

            throw new TestAssertionException(
                message ?? $"Expected exception of type {typeof(TException).Name}, but no exception was thrown");
        }

        public static async System.Threading.Tasks.Task<TException> ThrowsAsync<TException>(
            Func<System.Threading.Tasks.Task> action, string message = null) where TException : Exception
        {
            IsNotNull(action, "Action cannot be null");

            try
            {
                await action();
            }
            catch (TException ex)
            {
                return ex;
            }
            catch (Exception ex)
            {
                throw new TestAssertionException(
                    message ?? $"Expected exception of type {typeof(TException).Name}, but got {ex.GetType().Name}: {ex.Message}");
            }

            throw new TestAssertionException(
                message ?? $"Expected exception of type {typeof(TException).Name}, but no exception was thrown");
        }

        public static void Fail(string message = null)
        {
            throw new TestAssertionException(message ?? "Test failed");
        }

        private static string FormatValue(object value)
        {
            if (value == null)
                return "null";
            if (value is string str)
                return $"'{str}'";
            return value.ToString();
        }
    }
}

