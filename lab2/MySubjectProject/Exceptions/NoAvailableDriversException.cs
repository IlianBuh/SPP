using System;

namespace MySubjectProject.Exceptions
{
    public class NoAvailableDriversException : Exception
    {
        public NoAvailableDriversException() 
            : base("No available drivers found")
        {
        }

        public NoAvailableDriversException(string message) 
            : base(message)
        {
        }
    }
}

