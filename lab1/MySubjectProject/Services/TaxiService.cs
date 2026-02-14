using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySubjectProject.Models;
using MySubjectProject.Repositories;
using MySubjectProject.Exceptions;

namespace MySubjectProject.Services
{
    public class TaxiService : ITaxiService
    {
        private readonly IDriverService driverService;
        private readonly IPaymentService paymentService;
        private readonly IRideRepository rideRepository;

        public TaxiService(IDriverService driverService, IPaymentService paymentService, IRideRepository rideRepository)
        {
            this.driverService = driverService ?? throw new ArgumentNullException(nameof(driverService));
            this.paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            this.rideRepository = rideRepository ?? throw new ArgumentNullException(nameof(rideRepository));
        }

        public Ride CreateRide(string passengerName, string passengerPhone, string fromAddress, string toAddress, decimal distance)
        {
            if (string.IsNullOrWhiteSpace(passengerName))
                throw new ArgumentException("Passenger name cannot be empty");
            if (string.IsNullOrWhiteSpace(passengerPhone))
                throw new ArgumentException("Passenger phone cannot be empty");
            if (string.IsNullOrWhiteSpace(fromAddress))
                throw new ArgumentException("From address cannot be empty");
            if (string.IsNullOrWhiteSpace(toAddress))
                throw new ArgumentException("To address cannot be empty");
            if (distance <= 0)
                throw new ArgumentException("Distance must be positive");

            var availableDrivers = driverService.GetAvailableDrivers();
            if (availableDrivers.Count == 0)
                throw new NoAvailableDriversException();

            var cheapestDriver = availableDrivers.OrderBy(d => d.PricePerKm).First();
            var price = cheapestDriver.PricePerKm * distance;

            var allRides = rideRepository.GetAll();
            var ride = new Ride
            {
                Id = allRides.Count > 0 ? allRides.Max(r => r.Id) + 1 : 1,
                DriverId = cheapestDriver.Id,
                PassengerName = passengerName,
                PassengerPhone = passengerPhone,
                FromAddress = fromAddress,
                ToAddress = toAddress,
                Distance = distance,
                Price = price,
                Status = RideStatus.Pending,
                CreatedAt = DateTime.Now,
                IsPaid = false
            };

            rideRepository.Add(ride);
            driverService.UpdateDriverAvailability(cheapestDriver.Id, false);
            return ride;
        }

        public Ride AcceptRide(int rideId)
        {
            var ride = GetRideById(rideId);
            if (ride.Status != RideStatus.Pending)
                throw new InvalidRideStatusException(ride.Status, "accept");

            ride.Status = RideStatus.Accepted;
            rideRepository.Update(ride);
            return ride;
        }

        public Ride StartRide(int rideId)
        {
            var ride = GetRideById(rideId);
            if (ride.Status != RideStatus.Accepted)
                throw new InvalidRideStatusException(ride.Status, "start");

            ride.Status = RideStatus.InProgress;
            rideRepository.Update(ride);
            return ride;
        }

        public Ride CompleteRide(int rideId)
        {
            var ride = GetRideById(rideId);
            if (ride.Status != RideStatus.InProgress)
                throw new InvalidRideStatusException(ride.Status, "complete");

            ride.Status = RideStatus.Completed;
            ride.CompletedAt = DateTime.Now;
            rideRepository.Update(ride);
            driverService.UpdateDriverAvailability(ride.DriverId, true);
            return ride;
        }

        public Ride CancelRide(int rideId)
        {
            var ride = GetRideById(rideId);
            if (ride.Status == RideStatus.Completed)
                throw new InvalidRideStatusException(ride.Status, "cancel");

            ride.Status = RideStatus.Cancelled;
            rideRepository.Update(ride);
            driverService.UpdateDriverAvailability(ride.DriverId, true);
            return ride;
        }

        public async Task<Models.Payment> PayForRideAsync(int rideId, string paymentMethod)
        {
            var ride = GetRideById(rideId);
            if (ride.IsPaid)
                throw new InvalidOperationException("Ride is already paid");

            var payment = await paymentService.ProcessPaymentAsync(rideId, ride.Price, paymentMethod);
            ride.IsPaid = true;
            rideRepository.Update(ride);

            return payment;
        }

        public Ride GetRideById(int id)
        {
            var ride = rideRepository.GetById(id);
            if (ride == null)
                throw new RideNotFoundException(id);
            return ride;
        }

        public List<Ride> GetRidesByPassengerPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                throw new ArgumentException("Phone cannot be empty");
            return rideRepository.GetByPassengerPhone(phone);
        }

        public List<Ride> GetRidesByDriverId(int driverId)
        {
            return rideRepository.GetByDriverId(driverId);
        }

        public List<Ride> GetCompletedRides()
        {
            return rideRepository.GetAll().Where(r => r.Status == RideStatus.Completed).ToList();
        }

        public List<Ride> GetAllRides()
        {
            return rideRepository.GetAll();
        }

        public void Clear()
        {
            rideRepository.Clear();
        }
    }
}
