using System;

namespace MyTestFramework.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class InjectContextAttribute : Attribute
    {
    }
}

