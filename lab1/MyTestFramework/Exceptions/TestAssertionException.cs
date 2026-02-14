using System;

namespace MyTestFramework.Exceptions
{
    public class TestAssertionException : Exception
    {
        public TestAssertionException(string message) : base(message)
        {
        }

        public TestAssertionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

