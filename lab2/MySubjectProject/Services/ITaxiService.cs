using System.Collections.Generic;
using System.Threading.Tasks;
using MySubjectProject.Models;

namespace MySubjectProject.Services
{
    public interface ITaxiService
    {
        Ride CreateRide(string passengerName, string passengerPhone, string fromAddress, string toAddress, decimal distance);
        Ride AcceptRide(int rideId);
        Ride StartRide(int rideId);
        Ride CompleteRide(int rideId);
        Ride CancelRide(int rideId);
        Task<Models.Payment> PayForRideAsync(int rideId, string paymentMethod);
        Ride GetRideById(int id);
        List<Ride> GetRidesByPassengerPhone(string phone);
        List<Ride> GetRidesByDriverId(int driverId);
        List<Ride> GetCompletedRides();
        List<Ride> GetAllRides();
        void Clear();
    }
}

