using System;

namespace MyTestFramework.Exceptions
{
    public class TestConfigurationException : Exception
    {
        public TestConfigurationException(string message) : base(message)
        {
        }

        public TestConfigurationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

