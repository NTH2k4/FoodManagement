using FoodManagement.Contracts;
using FoodManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FoodManagement.Pages.Accounts.Admin
{
    public class CreateAdminModel : PageModel, ICreateView
    {
        private readonly Func<ICreateView, IPresenter<AdminDto>> _presenterFactory;
        private readonly IPasswordHasher _hasher;

        public CreateAdminModel(Func<ICreateView, IPresenter<AdminDto>> presenterFactory, IPasswordHasher hasher)
        {
            _presenterFactory = presenterFactory ?? throw new ArgumentNullException(nameof(presenterFactory));
            _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        }

        public class InputModel
        {
            [Required(ErrorMessage = "Tài khoản (username) là bắt buộc.")]
            [Display(Name = "Tài khoản (username)")]
            public string username { get; set; } = string.Empty;

            [Required(ErrorMessage = "Họ là bắt buộc.")]
            [Display(Name = "Họ")]
            public string firstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Tên là bắt buộc.")]
            [Display(Name = "Tên")]
            public string lastName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
            [MinLength(10, ErrorMessage = "Số điện thoại không đúng.")]
            [Phone]
            [Display(Name = "Số điện thoại")]
            public string phone { get; set; } = string.Empty;

            [EmailAddress]
            [Display(Name = "Email")]
            public string? email { get; set; }

            [Display(Name = "Địa chỉ")]
            public string? address { get; set; }

            [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
            [DataType(DataType.Password)]
            [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự.")]
            [Display(Name = "Mật khẩu")]
            public string password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Mật khẩu xác nhận không khớp.")]
            [DataType(DataType.Password)]
            [Compare("password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
            [Display(Name = "Xác nhận mật khẩu")]
            public string confirmPassword { get; set; } = string.Empty;

            [Display(Name = "Quyền")]
            public AdminRole role { get; set; } = AdminRole.Staff;

            [Display(Name = "Hoạt động")]
            public bool isActive { get; set; } = true;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [TempData]
        public string? Message { get; set; }
        [TempData]
        public string? Error { get; set; }

        public void OnGet()
        {
            Input.role = AdminRole.Staff;
            Input.isActive = true;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var (hashBase64, saltBase64) = _hasher.HashPassword(Input.password);

                var admin = new AdminDto
                {
                    id = Guid.NewGuid().ToString(),
                    username = Input.username.Trim(),
                    passwordHashBase64 = hashBase64,
                    passwordSaltBase64 = saltBase64,
                    phone = Input.phone.Trim(),
                    email = string.IsNullOrWhiteSpace(Input.email) ? null : Input.email.Trim(),
                    role = Input.role,
                    firstName = Input.firstName.Trim(),
                    lastName = Input.lastName.Trim(),
                    address = string.IsNullOrWhiteSpace(Input.address) ? null : Input.address.Trim(),
                    avatar = null,
                    createdAt = DateTime.UtcNow,
                    LastLoginAt = default,
                    LockoutEnd = default,
                    isActive = Input.isActive,
                    failedLoginAttempts = 0
                };

                var presenter = _presenterFactory(this);
                await presenter.CreateItemAsync(admin);

                if (!ModelState.IsValid || !string.IsNullOrEmpty(Error))
                {
                    return Page();
                }

                Message = "Tạo admin thành công.";
                await RedirectToListAsync();
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                Error = "Lỗi khi tạo admin: " + ex.Message;
                return Page();
            }
        }

        public void ShowMessage(string message)
        {
            Message = message;
        }

        public void ShowError(string error)
        {
            Error = error;
        }

        public void ShowValidationErrors(IDictionary<string, string> fieldErrors)
        {
            if (fieldErrors == null) return;
            foreach (var kv in fieldErrors)
            {
                ModelState.AddModelError(kv.Key ?? string.Empty, kv.Value ?? string.Empty);
            }
        }

        public Task RedirectToListAsync()
        {
            var url = Url.Page("./AdminPage") ?? "/";
            Response.Redirect(url);
            return Task.CompletedTask;
        }
    }
}
