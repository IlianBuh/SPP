using System;

namespace MyTestFramework.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class TestMethodAttribute : Attribute
    {
        public string DisplayName { get; set; }

        public bool Ignore { get; set; }

        public TestMethodAttribute()
        {
            DisplayName = null;
            Ignore = false;
        }
    }
}

