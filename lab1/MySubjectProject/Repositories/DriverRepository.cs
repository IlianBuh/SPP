using System.Collections.Generic;
using System.Linq;
using MySubjectProject.Models;

namespace MySubjectProject.Repositories
{
    public class DriverRepository : IDriverRepository
    {
        private List<Driver> drivers = new List<Driver>();

        public void Add(Driver driver)
        {
            drivers.Add(driver);
        }

        public Driver GetById(int id)
        {
            return drivers.FirstOrDefault(d => d.Id == id);
        }

        public List<Driver> GetAll()
        {
            return new List<Driver>(drivers);
        }

        public void Update(Driver driver)
        {
            var existing = GetById(driver.Id);
            if (existing != null)
            {
                var index = drivers.IndexOf(existing);
                drivers[index] = driver;
            }
        }

        public void Clear()
        {
            drivers.Clear();
        }
    }
}

