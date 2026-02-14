using System.Collections.Generic;
using System.Linq;
using MySubjectProject.Models;

namespace MySubjectProject.Repositories
{
    public class RideRepository : IRideRepository
    {
        private List<Ride> rides = new List<Ride>();

        public void Add(Ride ride)
        {
            rides.Add(ride);
        }

        public Ride GetById(int id)
        {
            return rides.FirstOrDefault(r => r.Id == id);
        }

        public List<Ride> GetAll()
        {
            return new List<Ride>(rides);
        }

        public List<Ride> GetByPassengerPhone(string phone)
        {
            return rides.Where(r => r.PassengerPhone == phone).ToList();
        }

        public List<Ride> GetByDriverId(int driverId)
        {
            return rides.Where(r => r.DriverId == driverId).ToList();
        }

        public void Update(Ride ride)
        {
            var existing = GetById(ride.Id);
            if (existing != null)
            {
                var index = rides.IndexOf(existing);
                rides[index] = ride;
            }
        }

        public void Clear()
        {
            rides.Clear();
        }
    }
}

