using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FoodManagement.Contracts;
using FoodManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace FoodManagement.Pages.Login
{
    public class ChangePasswordModel : PageModel
    {
        private readonly IService<AdminDto> _service;
        private readonly ILogger<ChangePasswordModel> _logger;

        public ChangePasswordModel(IService<AdminDto> service, ILogger<ChangePasswordModel> logger)
        {
            _service = service;
            _logger = logger;
        }

        [BindProperty]
        [Required]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Mật khẩu mới không được để trống.")]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "Mật khẩu mới phải có ít nhất 8 ký tự.")]
        public string NewPassword { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [TempData]
        public string? Message { get; set; }

        [TempData]
        public string? Error { get; set; }

        public AdminDto? Admin { get; private set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = GetUserIdFromClaims();
            if (string.IsNullOrEmpty(userId)) return RedirectToPage("/Login/Login");

            Admin = await _service.GetByIdAsync(userId);
            if (Admin == null) return RedirectToPage("/Login/Login");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Error = "Vui lòng kiểm tra lại thông tin.";
                return Page();
            }

            var userId = GetUserIdFromClaims();
            if (string.IsNullOrEmpty(userId)) return RedirectToPage("/Login/Login");

            Admin = await _service.GetByIdAsync(userId);
            if (Admin == null)
            {
                Error = "Không tìm thấy tài khoản.";
                return Page();
            }

            try
            {
                // Verify current password
                if (!VerifyPassword(CurrentPassword, Admin.passwordSaltBase64, Admin.passwordHashBase64))
                {
                    ModelState.AddModelError(nameof(CurrentPassword), "Mật khẩu hiện tại không đúng.");
                    return Page();
                }

                // Generate new salt + hash
                var newSalt = GenerateSalt();
                var newHash = HashPassword(NewPassword, newSalt);

                Admin.passwordSaltBase64 = Convert.ToBase64String(newSalt);
                Admin.passwordHashBase64 = Convert.ToBase64String(newHash);

                await _service.UpdateAsync(Admin);

                Message = "Đổi mật khẩu thành công.";
                return RedirectToPage("/Dashboard/Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while changing password for user {UserId}", userId);
                Error = "Đã xảy ra lỗi khi đổi mật khẩu. Vui lòng thử lại.";
                return Page();
            }
        }

        private string? GetUserIdFromClaims()
        {
            var cid = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(cid)) return cid;

            cid = User?.FindFirst("id")?.Value;
            if (!string.IsNullOrWhiteSpace(cid)) return cid;

            cid = User?.FindFirst("sub")?.Value;
            if (!string.IsNullOrWhiteSpace(cid)) return cid;

            return User?.Identity?.Name;
        }

        // --- Password helpers (PBKDF2 HMACSHA256) ---
        private static byte[] GenerateSalt(int length = 16)
        {
            var salt = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);
            return salt;
        }

        private static byte[] HashPassword(string password, byte[] salt, int iterations = 150000, int outBytes = 32)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(outBytes);
        }

        private static bool VerifyPassword(string providedPassword, string? saltBase64, string? hashBase64, int iterations = 150000)
        {
            if (string.IsNullOrEmpty(saltBase64) || string.IsNullOrEmpty(hashBase64)) return false;
            try
            {
                var salt = Convert.FromBase64String(saltBase64);
                var expected = Convert.FromBase64String(hashBase64);
                var actual = HashPassword(providedPassword, salt, iterations, expected.Length);
                return CryptographicOperations.FixedTimeEquals(expected, actual);
            }
            catch
            {
                return false;
            }
        }
    }
}
