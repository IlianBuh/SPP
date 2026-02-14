using System.Collections.Generic;
using System.Threading.Tasks;
using MySubjectProject.Models;

namespace MySubjectProject.Services
{
    public interface IPaymentService
    {
        Task<Models.Payment> ProcessPaymentAsync(int rideId, decimal amount, string paymentMethod);
        Models.Payment GetPaymentById(int id);
        List<Models.Payment> GetPaymentsByRideId(int rideId);
        List<Models.Payment> GetAllPayments();
        decimal GetTotalRevenue();
        void Clear();
    }
}

