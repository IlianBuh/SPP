using System;

namespace MyTestFramework.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class TimeoutAttribute : Attribute
    {
        public int TimeoutMilliseconds { get; }

        public TimeoutAttribute(int timeoutMilliseconds)
        {
            if (timeoutMilliseconds <= 0)
                throw new ArgumentException("Timeout must be a positive value", nameof(timeoutMilliseconds));
            TimeoutMilliseconds = timeoutMilliseconds;
        }
    }
}
