using System;
using System.Linq;
using System.Threading.Tasks;
using MyTestFramework;
using MyTestFramework.Attributes;
using MySubjectProject.Models;
using MySubjectProject.Services;
using MySubjectProject.Repositories;
using MySubjectProject.Payment;

namespace MyProject.Tests
{
    [TestClass]
    public class IntegrationTests
    {
        private TaxiService taxiService = default!;
        private DriverService driverService = default!;
        private PaymentService paymentService = default!;
        private IDriverRepository driverRepository = default!;
        private IPaymentRepository paymentRepository = default!;
        private IRideRepository rideRepository = default!;
        private IPaymentGateway paymentGateway = default!;

        [ClassInitialize]
        private static void ClassSetup()
        {
        }

        [ClassCleanup]
        private static void ClassCleanup()
        {
        }

        [InjectContext]
        public IContext Context { get; set; } = default!;

        [TestInitialize]
        private void Setup()
        {
            if (Context.ContainsData("TaxiService"))
            {

                taxiService = (TaxiService)Context.GetData("TaxiService");
                driverService = (DriverService)Context.GetData("DriverService");
                paymentService = (PaymentService)Context.GetData("PaymentService");
                rideRepository = (IRideRepository)Context.GetData("RideRepository");
                return;
            }


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


            Context.SetData("TaxiService", taxiService);
            Context.SetData("DriverService", driverService);
            Context.SetData("PaymentService", paymentService);
            Context.SetData("RideRepository", rideRepository);
        }

        [TestCleanup]
        private void Cleanup()
        {

            var drivers = driverService.GetAllDrivers();
            foreach (var driver in drivers)
            {
                if (!driver.IsAvailable)
                {
                    driverService.UpdateDriverAvailability(driver.Id, true);
                }
            }
        }

        [TestMethod]
        public void Integration_FullRideFlow_ShouldWork()
        {
            var ride = taxiService.CreateRide("Иван", "+375291111111", "Улица Ленина 1", "Улица Пушкина 10", 5m);
            Assert.AreEqual(RideStatus.Pending, ride.Status);

            var accepted = taxiService.AcceptRide(ride.Id);
            Assert.AreEqual(RideStatus.Accepted, accepted.Status);

            var started = taxiService.StartRide(ride.Id);
            Assert.AreEqual(RideStatus.InProgress, started.Status);

            var completed = taxiService.CompleteRide(ride.Id);
            Assert.AreEqual(RideStatus.Completed, completed.Status);
        }

        [TestMethod]
        public async Task Integration_RideWithPayment_ShouldCompleteSuccessfully()
        {
            var ride = taxiService.CreateRide("Иван", "+375291111111", "Улица Ленина 1", "Улица Пушкина 10", 5m);
            taxiService.AcceptRide(ride.Id);
            taxiService.StartRide(ride.Id);
            taxiService.CompleteRide(ride.Id);

            var payment = await taxiService.PayForRideAsync(ride.Id, "card");
            
            Assert.IsTrue(payment.IsSuccessful);
            var updatedRide = taxiService.GetRideById(ride.Id);
            Assert.IsTrue(updatedRide.IsPaid);
        }

        [TestMethod]
        public void Integration_FilterAndSortDrivers_ShouldWork()
        {
            var available = driverService.GetAvailableDrivers();
            var filtered = driverService.FilterDriversByRating(4.8);
            var sorted = driverService.SortDriversByPrice(true);

            Assert.IsTrue(available.Count > 0);
            Assert.IsTrue(filtered.All(d => d.Rating >= 4.8));
            Assert.IsTrue(sorted[0].PricePerKm <= sorted[1].PricePerKm);
        }

        [TestMethod]
        public void Integration_MultipleRides_ShouldTrackCorrectly()
        {
            var initialCount = taxiService.GetAllRides().Count;
            var initialIvanCount = taxiService.GetRidesByPassengerPhone("+375291111111").Count;

            var ride1 = taxiService.CreateRide("Иван", "+375291111111", "Улица Ленина 1", "Улица Пушкина 10", 5m);
            var ride2 = taxiService.CreateRide("Петр", "+375292222222", "Улица Мира 1", "Улица Мира 10", 3m);

            var allRides = taxiService.GetAllRides();
            Assert.AreEqual(initialCount + 2, allRides.Count);

            var ivanRides = taxiService.GetRidesByPassengerPhone("+375291111111");
            Assert.AreEqual(initialIvanCount + 1, ivanRides.Count);
        }

        [TestMethod]
        public async Task Integration_PaymentRevenue_ShouldCalculateCorrectly()
        {
            var initialRevenue = paymentService.GetTotalRevenue();

            var ride1 = taxiService.CreateRide("Иван", "+375291111111", "Улица Ленина 1", "Улица Пушкина 10", 5m);
            var ride2 = taxiService.CreateRide("Петр", "+375292222222", "Улица Мира 1", "Улица Мира 10", 3m);

            await taxiService.PayForRideAsync(ride1.Id, "card");
            await taxiService.PayForRideAsync(ride2.Id, "wallet");

            var revenue = paymentService.GetTotalRevenue();
            Assert.AreEqual(initialRevenue + ride1.Price + ride2.Price, revenue);
        }

        [TestMethod]
        public void Integration_DriverAvailability_ShouldUpdateAfterRide()
        {
            var driver = driverService.GetAvailableDrivers().First();
            var ride = taxiService.CreateRide("Иван", "+375291111111", "Улица Ленина 1", "Улица Пушкина 10", 5m);
            
            Assert.IsFalse(driverService.GetDriverById(driver.Id).IsAvailable);

            taxiService.AcceptRide(ride.Id);
            taxiService.StartRide(ride.Id);
            taxiService.CompleteRide(ride.Id);
            Assert.IsTrue(driverService.GetDriverById(driver.Id).IsAvailable);
        }
    }
}
