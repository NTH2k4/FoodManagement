namespace FoodManagement.Models
{
    public class BookingDto
    {
        public long id { get; set; }
        public string accountId { get; set; } = default!;
        public string address { get; set; } = default!;
        public int amount { get; set; }
        public long createdAt { get; set; }
        public string foods { get; set; } = default!;
        public string name { get; set; } = default!;
        public int payment { get; set; }
        public string paymentMethod { get; set; } = default!;
        public string phone { get; set; } = default!;
    }
}
