using System;

namespace MyTestFramework.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class TestInitializeAttribute : Attribute
    {
    }
}

