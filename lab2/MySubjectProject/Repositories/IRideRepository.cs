using System.Collections.Generic;
using MySubjectProject.Models;

namespace MySubjectProject.Repositories
{
    public interface IRideRepository
    {
        void Add(Ride ride);
        Ride GetById(int id);
        List<Ride> GetAll();
        List<Ride> GetByPassengerPhone(string phone);
        List<Ride> GetByDriverId(int driverId);
        void Update(Ride ride);
        void Clear();
    }
}

