namespace FoodManagement.Models
{
    public class FeedbackDto
    {
        public string id { get; set; } = default!;
        public string accountId { get; set; } = default!;
        public string? name { get; set; }
        public string? phone { get; set; }
        public string? email { get; set; }
        public string? comment { get; set; }
        public long createdAt { get; set; }
    }
}
