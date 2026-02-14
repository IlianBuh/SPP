using System;
using System.Linq;
using System.Threading.Tasks;
using MyTestFramework;
using MyTestFramework.Attributes;
using MySubjectProject.Models;
using MySubjectProject.Services;
using MySubjectProject.Repositories;
using MySubjectProject.Payment;
using MySubjectProject.Exceptions;

namespace MyProject.Tests
{
    [TestClass]
    public class TaxiServiceTests
    {
        private TaxiService taxiService;
        private DriverService driverService;
        private PaymentService paymentService;
        private IDriverRepository driverRepository;
        private IPaymentRepository paymentRepository;
        private IRideRepository rideRepository;
        private IPaymentGateway paymentGateway;

        [TestInitialize]
        private void Setup()
        {
            driverRepository = new DriverRepository();
            paymentRepository = new PaymentRepository();
            rideRepository = new RideRepository();
            paymentGateway = new PaymentGateway();
            
            driverService = new DriverService(driverRepository);
            paymentService = new PaymentService(paymentRepository, paymentGateway);
            taxiService = new TaxiService(driverService, paymentService, rideRepository);

            driverService.AddDriver(new Driver
            {
                Name = "Иван Иванов",
                Phone = "+375291234567",
                CarModel = "Toyota Camry",
                CarNumber = "1234 AB-7",
                Rating = 4.8,
                IsAvailable = true,
                PricePerKm = 1.5m,
                CarType = "Economy"
            });

            driverService.AddDriver(new Driver
            {
                Name = "Петр Петров",
                Phone = "+375292345678",
                CarModel = "Mercedes E-Class",
                CarNumber = "5678 CD-7",
                Rating = 4.9,
                IsAvailable = true,
                PricePerKm = 2.5m,
                CarType = "Premium"
            });
        }

        [TestCleanup]
        private void Cleanup()
        {
            taxiService.Clear();
            driverService.Clear();
            paymentService.Clear();
        }

        [TestMethod]
        public void CreateRide_ShouldCreateRide()
        {
            var ride = taxiService.CreateRide("Иван", "+375291111111", "Улица Ленина 1", "Улица Пушкина 10", 5m);
            
            Assert.IsNotNull(ride);
            Assert.AreEqual(RideStatus.Pending, ride.Status);
            Assert.AreEqual(5m, ride.Distance);
            Assert.AreEqual(7.5m, ride.Price);
            Assert.IsFalse(ride.IsPaid);
        }

        [TestMethod]
        public void CreateRide_ShouldSelectCheapestDriver()
        {
            var ride = taxiService.CreateRide("Иван", "+375291111111", "Улица Ленина 1", "Улица Пушкина 10", 5m);
            var driver = driverService.GetDriverById(ride.DriverId);
            Assert.AreEqual(1.5m, driver.PricePerKm);
        }

        [TestMethod]
        public void CreateRide_WithEmptyPassengerName_ShouldThrowArgumentException()
        {
            Assert.Throws<ArgumentException>(() => 
                taxiService.CreateRide("", "+375291111111", "Улица Ленина 1", "Улица Пушкина 10", 5m));
        }

        [TestMethod]
        public void CreateRide_WithNoAvailableDrivers_ShouldThrowNoAvailableDriversException()
        {
            driverService.UpdateDriverAvailability(1, false);
            driverService.UpdateDriverAvailability(2, false);
            
            Assert.Throws<NoAvailableDriversException>(() => 
                taxiService.CreateRide("Иван", "+375291111111", "Улица Ленина 1", "Улица Пушкина 10", 5m));
        }

        [TestMethod]
        [DataRow("Иван", "+375291111111", "Улица Ленина 1", "Улица Пушкина 10", 5.0, 7.5)]
        [DataRow("Петр", "+375292222222", "Улица Мира 5", "Улица Мира 20", 10.0, 15.0)]
        public void CreateRide_ShouldCalculatePriceCorrectly(string name, string phone, string from, string to, double distance, double expectedPrice)
        {
            var ride = taxiService.CreateRide(name, phone, from, to, (decimal)distance);
            Assert.AreEqual((decimal)expectedPrice, ride.Price);
        }

        [TestMethod]
        public void AcceptRide_ShouldChangeStatusToAccepted()
        {
            var ride = taxiService.CreateRide("Иван", "+375291111111", "Улица Ленина 1", "Улица Пушкина 10", 5m);
            var accepted = taxiService.AcceptRide(ride.Id);
            Assert.AreEqual(RideStatus.Accepted, accepted.Status);
        }

        [TestMethod]
        public void StartRide_ShouldChangeStatusToInProgress()
        {
            var ride = taxiService.CreateRide("Иван", "+375291111111", "Улица Ленина 1", "Улица Пушкина 10", 5m);
            taxiService.AcceptRide(ride.Id);
            var started = taxiService.StartRide(ride.Id);
            Assert.AreEqual(RideStatus.InProgress, started.Status);
        }

        [TestMethod]
        public void CompleteRide_ShouldChangeStatusToCompleted()
        {
            var ride = taxiService.CreateRide("Иван", "+375291111111", "Улица Ленина 1", "Улица Пушкина 10", 5m);
            taxiService.AcceptRide(ride.Id);
            taxiService.StartRide(ride.Id);
            var completed = taxiService.CompleteRide(ride.Id);
            
            Assert.AreEqual(RideStatus.Completed, completed.Status);
            Assert.IsNotNull(completed.CompletedAt);
        }

        [TestMethod]
        public void CancelRide_ShouldChangeStatusToCancelled()
        {
            var ride = taxiService.CreateRide("Иван", "+375291111111", "Улица Ленина 1", "Улица Пушкина 10", 5m);
            var cancelled = taxiService.CancelRide(ride.Id);
            Assert.AreEqual(RideStatus.Cancelled, cancelled.Status);
        }

        [TestMethod]
        public async Task PayForRideAsync_WithCard_ShouldMarkAsPaid()
        {
            var ride = taxiService.CreateRide("Иван", "+375291111111", "Улица Ленина 1", "Улица Пушкина 10", 5m);
            var payment = await taxiService.PayForRideAsync(ride.Id, "card");
            
            var updatedRide = taxiService.GetRideById(ride.Id);
            Assert.IsTrue(updatedRide.IsPaid);
            Assert.IsTrue(payment.IsSuccessful);
        }

        [TestMethod]
        public async Task PayForRideAsync_WithCash_ShouldThrowPaymentFailedException()
        {
            var ride = taxiService.CreateRide("Иван", "+375291111111", "Улица Ленина 1", "Улица Пушкина 10", 5m);
            await Assert.ThrowsAsync<PaymentFailedException>(() => 
                taxiService.PayForRideAsync(ride.Id, "cash"));
        }

        [TestMethod]
        public void GetRidesByPassengerPhone_ShouldReturnRides()
        {
            var ride1 = taxiService.CreateRide("Иван", "+375291111111", "Улица Ленина 1", "Улица Пушкина 10", 5m);
            taxiService.CancelRide(ride1.Id);
            
            var ride2 = taxiService.CreateRide("Иван", "+375291111111", "Улица Мира 1", "Улица Мира 10", 3m);
            taxiService.CancelRide(ride2.Id);
            
            var ride3 = taxiService.CreateRide("Петр", "+375292222222", "Улица Ленина 1", "Улица Пушкина 10", 5m);
            taxiService.CancelRide(ride3.Id);

            var rides = taxiService.GetRidesByPassengerPhone("+375291111111");
            Assert.AreEqual(2, rides.Count);
        }

        [TestMethod]
        public void GetCompletedRides_ShouldReturnOnlyCompleted()
        {
            var ride1 = taxiService.CreateRide("Иван", "+375291111111", "Улица Ленина 1", "Улица Пушкина 10", 5m);
            var ride2 = taxiService.CreateRide("Петр", "+375292222222", "Улица Мира 1", "Улица Мира 10", 3m);

            taxiService.AcceptRide(ride1.Id);
            taxiService.StartRide(ride1.Id);
            taxiService.CompleteRide(ride1.Id);

            var completed = taxiService.GetCompletedRides();
            Assert.AreEqual(1, completed.Count);
            Assert.AreEqual(ride1.Id, completed[0].Id);
        }

        [TestMethod]
        public void GetRideById_WithInvalidId_ShouldThrowRideNotFoundException()
        {
            Assert.Throws<RideNotFoundException>(() => taxiService.GetRideById(999));
        }

        [TestMethod]
        public void AcceptRide_WithInvalidStatus_ShouldThrowInvalidRideStatusException()
        {
            var ride = taxiService.CreateRide("Иван", "+375291111111", "Улица Ленина 1", "Улица Пушкина 10", 5m);
            taxiService.AcceptRide(ride.Id);
            taxiService.StartRide(ride.Id);
            
            Assert.Throws<InvalidRideStatusException>(() => taxiService.AcceptRide(ride.Id));
        }

        [TestMethod]
        public void StartRide_WithInvalidStatus_ShouldThrowInvalidRideStatusException()
        {
            var ride = taxiService.CreateRide("Иван", "+375291111111", "Улица Ленина 1", "Улица Пушкина 10", 5m);
            
            Assert.Throws<InvalidRideStatusException>(() => taxiService.StartRide(ride.Id));
        }

        [TestMethod]
        public void CompleteRide_WithInvalidStatus_ShouldThrowInvalidRideStatusException()
        {
            var ride = taxiService.CreateRide("Иван", "+375291111111", "Улица Ленина 1", "Улица Пушкина 10", 5m);
            
            Assert.Throws<InvalidRideStatusException>(() => taxiService.CompleteRide(ride.Id));
        }

        [TestMethod]
        public void CancelRide_WhenCompleted_ShouldThrowInvalidRideStatusException()
        {
            var ride = taxiService.CreateRide("Иван", "+375291111111", "Улица Ленина 1", "Улица Пушкина 10", 5m);
            taxiService.AcceptRide(ride.Id);
            taxiService.StartRide(ride.Id);
            taxiService.CompleteRide(ride.Id);
            
            Assert.Throws<InvalidRideStatusException>(() => taxiService.CancelRide(ride.Id));
        }

        [TestMethod]
        public void CreateRide_CompletedAtShouldBeNull()
        {
            var ride = taxiService.CreateRide("Иван", "+375291111111", "Улица Ленина 1", "Улица Пушкина 10", 5m);
            Assert.IsNull(ride.CompletedAt);
        }

        [TestMethod]
        public void GetRidesByPassengerPhone_ShouldContainCreatedRideId()
        {
            var ride = taxiService.CreateRide("Иван", "+375291111111", "Улица Ленина 1", "Улица Пушкина 10", 5m);
            var rides = taxiService.GetRidesByPassengerPhone("+375291111111");
            var rideIds = rides.Select(r => r.Id).ToList();
            Assert.Contains(ride.Id, rideIds);
        }
    }
}
