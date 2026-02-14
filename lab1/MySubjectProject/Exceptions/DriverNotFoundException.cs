using System;

namespace MySubjectProject.Exceptions
{
    public class DriverNotFoundException : Exception
    {
        public DriverNotFoundException(int driverId) 
            : base($"Driver with id {driverId} not found")
        {
        }

        public DriverNotFoundException(string message) 
            : base(message)
        {
        }
    }
}

