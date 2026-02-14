using System;
using MySubjectProject.Models;

namespace MySubjectProject.Exceptions
{
    public class InvalidRideStatusException : Exception
    {
        public InvalidRideStatusException(RideStatus currentStatus, string operation) 
            : base($"Cannot {operation} ride with status {currentStatus}")
        {
        }

        public InvalidRideStatusException(string message) 
            : base(message)
        {
        }
    }
}

