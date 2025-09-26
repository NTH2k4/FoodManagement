using FoodManagement.Validation;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace FoodManagement.Models
{
    public class UserDto
    {
        [BindNever]
        public string? id { get; set; } 

        [Required(ErrorMessage = "Họ tên là bắt buộc.")]
        public string fullName { get; set; } = default!;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        [MinLength(10, ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string phone { get; set; } = default!;

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string email { get; set; } = default!;

        [Required(ErrorMessage = "Địa chỉ là bắt buộc.")]
        public string address { get; set; } = string.Empty;

        [RequiredOnCreate(ErrorMessage = "Mật khẩu là bắt buộc khi tạo tài khoản.")]
        public string? password { get; set; }

        public long createdAt { get; set; } = (long)System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 60;
    }
}
