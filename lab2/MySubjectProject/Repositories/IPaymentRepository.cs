using System.Collections.Generic;
using MySubjectProject.Models;

namespace MySubjectProject.Repositories
{
    public interface IPaymentRepository
    {
        void Add(Models.Payment payment);
        Models.Payment GetById(int id);
        List<Models.Payment> GetAll();
        List<Models.Payment> GetByRideId(int rideId);
        void Update(Models.Payment payment);
        void Clear();
    }
}

