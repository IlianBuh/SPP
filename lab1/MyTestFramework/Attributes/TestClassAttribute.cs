using System;

namespace MyTestFramework.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class TestClassAttribute : Attribute
    {
    }
}

