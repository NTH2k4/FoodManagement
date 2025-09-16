using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FoodManagement.Models
{
    public class UserDto
    {
        [BindNever]
        public string? id { get; set; } 
        [Required(ErrorMessage = "Họ tên là bắt buộc.")]
        public string fullName { get; set; } = default!;
        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        public string phone { get; set; } = default!;
        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string email { get; set; } = default!;
        public string address { get; set; } = string.Empty;
        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        public string password { get; set; } = default!;
        public long createdAt { get; set; } = (long)System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 60;
    }
}
