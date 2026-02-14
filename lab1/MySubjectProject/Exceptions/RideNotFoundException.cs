using System;

namespace MySubjectProject.Exceptions
{
    public class RideNotFoundException : Exception
    {
        public RideNotFoundException(int rideId) 
            : base($"Ride with id {rideId} not found")
        {
        }

        public RideNotFoundException(string message) 
            : base(message)
        {
        }
    }
}

