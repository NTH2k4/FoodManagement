namespace FoodManagement.Models
{
    public enum StatisticsRange
    {
        Day,
        Month
    }

    public class PaymentMethodStat
    {
        public string Method { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Total { get; set; }
    }

    public class TopFoodStat
    {
        public string Name { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public int TotalQuantity { get; set; }
    }

    public class TopUserStat
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public decimal TotalSpent { get; set; }
    }
}
