using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using FoodManagement.Contracts;
using FoodManagement.Models;
using FoodManagement.Presenters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace FoodManagement.Pages.Login
{
    public class ChangePasswordModel : PageModel, IEditView<AdminDto>
    {
        private readonly Func<IEditView<AdminDto>, IPresenter<AdminDto>> _presenterFactory;
        private IPresenter<AdminDto>? _presenter;
        private readonly ILogger<ChangePasswordModel> _logger;

        public ChangePasswordModel(Func<IEditView<AdminDto>, IPresenter<AdminDto>> presenterFactory, ILogger<ChangePasswordModel> logger)
        {
            _presenterFactory = presenterFactory ?? throw new ArgumentNullException(nameof(presenterFactory));
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

            _presenter ??= _presenterFactory(this);

            try
            {
                if (_presenter is AdminPresenter ap)
                {
                    await ap.ChangePasswordAsync(userId, CurrentPassword, NewPassword);
                    Console.WriteLine("OKKKK", userId);
                }
                else
                {
                    await _presenter.LoadItemByIdAsync(userId);
                    Console.WriteLine("Wrong...", userId);
                }

                TempData["Message"] = "Đổi mật khẩu thành công.";
                return RedirectToPage("/Login/Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while changing password for user {UserId}", userId);
                Error = "Đổi mật khẩu thất bại: " + ex.Message;
                return Page();
            }
        }

        public void ShowItemDetail(AdminDto item)
        {
            Admin = item;
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
            Response.Redirect(Url.Page("/Login/Account") ?? "/");
            return Task.CompletedTask;
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
    }
}
