using System;

namespace MyTestFramework.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class DataRowAttribute : Attribute
    {
        public object[] Data { get; }

        public DataRowAttribute(params object[] data)
        {
            Data = data ?? new object[0];
        }
    }
}

