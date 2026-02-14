using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySubjectProject.Models;
using MySubjectProject.Repositories;
using MySubjectProject.Payment;
using MySubjectProject.Exceptions;

namespace MySubjectProject.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository paymentRepository;
        private readonly IPaymentGateway paymentGateway;

        public PaymentService(IPaymentRepository paymentRepository, IPaymentGateway paymentGateway)
        {
            this.paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
            this.paymentGateway = paymentGateway ?? throw new ArgumentNullException(nameof(paymentGateway));
        }

        public async Task<Models.Payment> ProcessPaymentAsync(int rideId, decimal amount, string paymentMethod)
        {
            if (rideId <= 0)
                throw new ArgumentException("Ride ID must be positive");
            if (amount <= 0)
                throw new ArgumentException("Amount must be positive");
            if (string.IsNullOrWhiteSpace(paymentMethod))
                throw new ArgumentException("Payment method cannot be empty");

            var transactionId = Guid.NewGuid().ToString();
            var gatewayResult = await paymentGateway.ProcessPaymentAsync(amount, paymentMethod, transactionId);

            var allPayments = paymentRepository.GetAll();
            var payment = new Models.Payment
            {
                Id = allPayments.Count > 0 ? allPayments.Max(p => p.Id) + 1 : 1,
                RideId = rideId,
                Amount = amount,
                PaymentMethod = paymentMethod,
                ProcessedAt = DateTime.Now,
                TransactionId = gatewayResult.TransactionId,
                IsSuccessful = gatewayResult.IsSuccessful
            };

            paymentRepository.Add(payment);

            if (!gatewayResult.IsSuccessful)
            {
                throw new PaymentFailedException(gatewayResult.ErrorMessage ?? "Payment processing failed");
            }

            return payment;
        }

        public Models.Payment GetPaymentById(int id)
        {
            var payment = paymentRepository.GetById(id);
            if (payment == null)
                throw new PaymentNotFoundException(id);
            return payment;
        }

        public List<Models.Payment> GetPaymentsByRideId(int rideId)
        {
            return paymentRepository.GetByRideId(rideId);
        }

        public List<Models.Payment> GetAllPayments()
        {
            return paymentRepository.GetAll();
        }

        public decimal GetTotalRevenue()
        {
            return paymentRepository.GetAll().Where(p => p.IsSuccessful).Sum(p => p.Amount);
        }

        public void Clear()
        {
            paymentRepository.Clear();
        }
    }
}
