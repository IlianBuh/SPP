namespace MySubjectProject.Models
{
    public class Driver
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string CarModel { get; set; }
        public string CarNumber { get; set; }
        public double Rating { get; set; }
        public bool IsAvailable { get; set; }
        public decimal PricePerKm { get; set; }
        public string CarType { get; set; }
    }
}

