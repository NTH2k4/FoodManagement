namespace FoodManagement.Models
{
    public enum AdminRole
    {
        Staff = 0,
        SuperAdmin = 1
    }
    public class AdminDto
    {
        public string id { get; set; } = default!;
        public string username { get; set; } = default!;
        public string passwordHashBase64 { get; set; } = default!;
        public string passwordSaltBase64 { get; set; } = default!;
        public string phone { get; set; } = default!;
        public string? email { get; set; }
        public AdminRole role { get; set; }
        public string firstName { get; set; } = default!;
        public string lastName { get; set; } = default!;
        public string? address { get; set; }
        public string? avatar { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime LastLoginAt { get; set; }
        public DateTime LockoutEnd { get; set; }
        public bool isActive { get; set; }
        public int failedLoginAttempts { get; set; } = 0;
    }
}
