using System.Collections.Generic;
using System.Linq;
using MySubjectProject.Models;

namespace MySubjectProject.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private List<Models.Payment> payments = new List<Models.Payment>();

        public void Add(Models.Payment payment)
        {
            payments.Add(payment);
        }

        public Models.Payment GetById(int id)
        {
            return payments.FirstOrDefault(p => p.Id == id);
        }

        public List<Models.Payment> GetAll()
        {
            return new List<Models.Payment>(payments);
        }

        public List<Models.Payment> GetByRideId(int rideId)
        {
            return payments.Where(p => p.RideId == rideId).ToList();
        }

        public void Update(Models.Payment payment)
        {
            var existing = GetById(payment.Id);
            if (existing != null)
            {
                var index = payments.IndexOf(existing);
                payments[index] = payment;
            }
        }

        public void Clear()
        {
            payments.Clear();
        }
    }
}

