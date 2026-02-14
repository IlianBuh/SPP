using System;

namespace MySubjectProject.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int RideId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public bool IsSuccessful { get; set; }
        public DateTime ProcessedAt { get; set; }
        public string TransactionId { get; set; }
    }
}

