using System.Collections.Generic;
using MySubjectProject.Models;

namespace MySubjectProject.Services
{
    public interface IDriverService
    {
        void AddDriver(Driver driver);
        List<Driver> GetAllDrivers();
        Driver GetDriverById(int id);
        List<Driver> GetAvailableDrivers();
        List<Driver> FilterDriversByRating(double minRating);
        List<Driver> FilterDriversByCarType(string carType);
        List<Driver> SortDriversByRating(bool ascending = false);
        List<Driver> SortDriversByPrice(bool ascending = true);
        void UpdateDriverAvailability(int driverId, bool isAvailable);
        void Clear();
    }
}

