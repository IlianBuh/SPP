using System;
using System.Linq;
using MyTestFramework;
using MyTestFramework.Attributes;
using MySubjectProject.Models;
using MySubjectProject.Services;
using MySubjectProject.Repositories;
using MySubjectProject.Exceptions;

namespace MyProject.Tests
{
    [TestClass]
    public class DriverServiceTests
    {
        private DriverService driverService;
        private IDriverRepository driverRepository;

        [TestInitialize]
        private void Setup()
        {
            driverRepository = new DriverRepository();
            driverService = new DriverService(driverRepository);
            
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

            driverService.AddDriver(new Driver
            {
                Name = "Сергей Сергеев",
                Phone = "+375293456789",
                CarModel = "BMW 5 Series",
                CarNumber = "9012 EF-7",
                Rating = 4.5,
                IsAvailable = false,
                PricePerKm = 1.2m,
                CarType = "Economy"
            });
        }

        [TestCleanup]
        private void Cleanup()
        {
            driverService.Clear();
        }

        [TestMethod]
        public void AddDriver_ShouldAddDriver()
        {
            var driver = new Driver
            {
                Name = "Новый Водитель",
                Phone = "+375294567890",
                CarModel = "Lada",
                CarNumber = "1111 AA-7",
                Rating = 4.0,
                IsAvailable = true,
                PricePerKm = 1.0m,
                CarType = "Economy"
            };

            driverService.AddDriver(driver);
            var allDrivers = driverService.GetAllDrivers();
            Assert.AreEqual(4, allDrivers.Count);
        }

        [TestMethod]
        public void AddDriver_WithNullDriver_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => driverService.AddDriver(null));
        }

        [TestMethod]
        public void AddDriver_WithEmptyName_ShouldThrowArgumentException()
        {
            var driver = new Driver { Name = "", Rating = 4.0, PricePerKm = 1.0m };
            Assert.Throws<ArgumentException>(() => driverService.AddDriver(driver));
        }

        [TestMethod]
        public void AddDriver_WithInvalidRating_ShouldThrowArgumentException()
        {
            var driver = new Driver { Name = "Test", Rating = 6.0, PricePerKm = 1.0m };
            Assert.Throws<ArgumentException>(() => driverService.AddDriver(driver));
        }

        [TestMethod]
        public void GetAvailableDrivers_ShouldReturnOnlyAvailable()
        {
            var available = driverService.GetAvailableDrivers();
            Assert.AreEqual(2, available.Count);
            Assert.IsTrue(available.All(d => d.IsAvailable));
        }

        [TestMethod]
        public void FilterDriversByRating_ShouldReturnFiltered()
        {
            var filtered = driverService.FilterDriversByRating(4.8);
            Assert.AreEqual(2, filtered.Count);
            Assert.IsTrue(filtered.All(d => d.Rating >= 4.8));
        }

        [TestMethod]
        [DataRow("Economy", 2)]
        [DataRow("Premium", 1)]
        [DataRow("Luxury", 0)]
        public void FilterDriversByCarType_ShouldReturnFiltered(string carType, int expectedCount)
        {
            var filtered = driverService.FilterDriversByCarType(carType);
            Assert.AreEqual(expectedCount, filtered.Count);
        }

        [TestMethod]
        public void SortDriversByRating_Descending_ShouldSortCorrectly()
        {
            var sorted = driverService.SortDriversByRating(false);
            Assert.IsTrue(sorted[0].Rating >= sorted[1].Rating);
            Assert.IsTrue(sorted[1].Rating >= sorted[2].Rating);
        }

        [TestMethod]
        public void SortDriversByPrice_Ascending_ShouldSortCorrectly()
        {
            var sorted = driverService.SortDriversByPrice(true);
            Assert.IsTrue(sorted[0].PricePerKm <= sorted[1].PricePerKm);
        }

        [TestMethod]
        public void GetDriverById_ShouldReturnDriver()
        {
            var driver = driverService.GetDriverById(1);
            Assert.IsNotNull(driver);
            Assert.AreEqual(1, driver.Id);
        }

        [TestMethod]
        public void GetDriverById_WithInvalidId_ShouldThrowDriverNotFoundException()
        {
            Assert.Throws<DriverNotFoundException>(() => driverService.GetDriverById(999));
        }

        [TestMethod]
        public void UpdateDriverAvailability_ShouldUpdateStatus()
        {
            driverService.UpdateDriverAvailability(1, false);
            var driver = driverService.GetDriverById(1);
            Assert.IsFalse(driver.IsAvailable);
        }

        [TestMethod]
        public void GetAllDrivers_ShouldHaveDistinctIds()
        {
            var drivers = driverService.GetAllDrivers();
            Assert.AreNotEqual(drivers[0].Id, drivers[1].Id);
        }

        [TestMethod]
        public void GetDriverById_NameShouldContainSubstring()
        {
            var driver = driverService.GetDriverById(1);
            Assert.Contains("Иванов", driver.Name);
        }

        [TestMethod]
        public void GetDriverById_PhoneShouldStartWithCountryCode()
        {
            var driver = driverService.GetDriverById(1);
            Assert.StartsWith("+375", driver.Phone);
        }

        [TestMethod]
        public void GetAllDrivers_AllItemsShouldBeNotNull()
        {
            var drivers = driverService.GetAllDrivers();
            Assert.AllItemsAreNotNull(drivers);
        }

        [TestMethod]
        public void FilterDriversByCarType_UnknownType_ShouldReturnEmpty()
        {
            var filtered = driverService.FilterDriversByCarType("Helicopter");
            Assert.IsEmpty(filtered);
            var names = string.Join("", filtered.Select(d => d.Name));
            Assert.IsEmpty(names);
        }
    }
}
