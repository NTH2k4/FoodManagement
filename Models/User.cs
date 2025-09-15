namespace FoodManagement.Models
{
    public class User
    {
        //var id: String? = null,
        //var fullName: String? = null,
        //var phone: String? = null,
        //var email: String? = null,
        //var address: String? = null,
        //var password: String? = null,
        //var province: String? = null,
        //var district: String? = null,
        //var createdAt: Long = System.currentTimeMillis()

        public string Id { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public string Phone { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Address { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string? Province { get; set; }
        public string? District { get; set; }
        public long CreatedAt { get; set; } = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
