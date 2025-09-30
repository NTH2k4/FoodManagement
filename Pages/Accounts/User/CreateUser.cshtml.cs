using FoodManagement.Contracts;
using FoodManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Threading.Tasks;

namespace FoodManagement.Pages.Accounts.User
{
    public class CreateUserModel : PageModel, ICreateView
    {
        private readonly Func<ICreateView, IPresenter<UserDto>> _presenterFactory;

        public CreateUserModel(Func<ICreateView, IPresenter<UserDto>> presenterFactory)
        {
            _presenterFactory = presenterFactory ?? throw new ArgumentNullException(nameof(presenterFactory));
        }

        [BindProperty]
        public UserDto User { get; set; } = new();

        [TempData]
        public string? Message { get; set; }
        [TempData]
        public string? Error { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Error = "Vui lòng kiểm tra lại thông tin.";
                return Page();
            }

            var presenter = _presenterFactory(this);
            try
            {
                await presenter.CreateItemAsync(User);
                if (!string.IsNullOrEmpty(Error)) return Page();
                Message = "Tạo tài khoản thành công.";
                return RedirectToPage("./UserPage");
            }
            catch (Exception ex)
            {
                Error = $"Lỗi khi tạo tài khoản: {ex.Message}";
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

        public void ShowValidationErrors(System.Collections.Generic.IDictionary<string, string> fieldErrors)
        {
            if (fieldErrors == null) return;
            foreach (var kv in fieldErrors)
            {
                ModelState.AddModelError(kv.Key ?? string.Empty, kv.Value ?? string.Empty);
            }
        }

        public Task RedirectToListAsync()
        {
            Response.Redirect(Url.Page("./UserPage") ?? "/");
            return Task.CompletedTask;
        }
    }
}
