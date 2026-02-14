using System;

namespace MySubjectProject.Models
{
    public class Ride
    {
        public int Id { get; set; }
        public int DriverId { get; set; }
        public string PassengerName { get; set; }
        public string PassengerPhone { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public decimal Distance { get; set; }
        public decimal Price { get; set; }
        public RideStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsPaid { get; set; }
    }
}

