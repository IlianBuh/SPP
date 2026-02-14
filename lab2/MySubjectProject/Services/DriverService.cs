using System;
using System.Collections.Generic;
using System.Linq;
using MySubjectProject.Models;
using MySubjectProject.Repositories;
using MySubjectProject.Exceptions;

namespace MySubjectProject.Services
{
    public class DriverService : IDriverService
    {
        private readonly IDriverRepository driverRepository;

        public DriverService(IDriverRepository driverRepository)
        {
            this.driverRepository = driverRepository ?? throw new ArgumentNullException(nameof(driverRepository));
        }

        public void AddDriver(Driver driver)
        {
            if (driver == null)
                throw new ArgumentNullException(nameof(driver));
            if (string.IsNullOrWhiteSpace(driver.Name))
                throw new ArgumentException("Driver name cannot be empty");
            if (driver.Rating < 0 || driver.Rating > 5)
                throw new ArgumentException("Rating must be between 0 and 5");
            if (driver.PricePerKm <= 0)
                throw new ArgumentException("Price per km must be positive");

            var allDrivers = driverRepository.GetAll();
            driver.Id = allDrivers.Count > 0 ? allDrivers.Max(d => d.Id) + 1 : 1;
            driverRepository.Add(driver);
        }

        public List<Driver> GetAllDrivers()
        {
            return driverRepository.GetAll();
        }

        public Driver GetDriverById(int id)
        {
            var driver = driverRepository.GetById(id);
            if (driver == null)
                throw new DriverNotFoundException(id);
            return driver;
        }

        public List<Driver> GetAvailableDrivers()
        {
            return driverRepository.GetAll().Where(d => d.IsAvailable).ToList();
        }

        public List<Driver> FilterDriversByRating(double minRating)
        {
            if (minRating < 0 || minRating > 5)
                throw new ArgumentException("Rating must be between 0 and 5");
            return driverRepository.GetAll().Where(d => d.Rating >= minRating).ToList();
        }

        public List<Driver> FilterDriversByCarType(string carType)
        {
            if (string.IsNullOrWhiteSpace(carType))
                throw new ArgumentException("Car type cannot be empty");
            return driverRepository.GetAll().Where(d => d.CarType.Equals(carType, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public List<Driver> SortDriversByRating(bool ascending = false)
        {
            var drivers = driverRepository.GetAll();
            return ascending
                ? drivers.OrderBy(d => d.Rating).ToList()
                : drivers.OrderByDescending(d => d.Rating).ToList();
        }

        public List<Driver> SortDriversByPrice(bool ascending = true)
        {
            var drivers = driverRepository.GetAll();
            return ascending
                ? drivers.OrderBy(d => d.PricePerKm).ToList()
                : drivers.OrderByDescending(d => d.PricePerKm).ToList();
        }

        public void UpdateDriverAvailability(int driverId, bool isAvailable)
        {
            var driver = GetDriverById(driverId);
            driver.IsAvailable = isAvailable;
            driverRepository.Update(driver);
        }

        public void Clear()
        {
            driverRepository.Clear();
        }
    }
}
