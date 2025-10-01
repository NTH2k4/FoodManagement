using FoodManagement.Contracts;
using FoodManagement.Models;
using FoodManagement.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace FoodManagement.Pages.Accounts.Admin
{
    public class EditAdminModel : PageModel, IEditView<AdminDto>
    {
        private readonly Func<IEditView<AdminDto>, IPresenter<AdminDto>> _presenterFactory;
        private readonly IPasswordHasher _hasher;
        private IPresenter<AdminDto>? _presenter;

        public EditAdminModel(Func<IEditView<AdminDto>, IPresenter<AdminDto>> presenterFactory, IPasswordHasher hasher)
        {
            _presenterFactory = presenterFactory;
            _hasher = hasher;
        }

        [BindProperty]
        public AdminDto Admin { get; set; } = new();

        [BindProperty]
        public string? PlainPassword { get; set; }

        [BindProperty]
        public string? ConfirmPassword { get; set; }

        [TempData]
        public string? Message { get; set; }

        [TempData]
        public string? Error { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            _presenter ??= _presenterFactory(this);
            await _presenter.LoadItemByIdAsync(id);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("Admin.passwordHashBase64");
            ModelState.Remove("Admin.passwordSaltBase64");

            if (!ModelState.IsValid) return Page();

            if (!string.IsNullOrWhiteSpace(PlainPassword))
            {
                if (string.IsNullOrWhiteSpace(ConfirmPassword) || PlainPassword != ConfirmPassword)
                {
                    ModelState.AddModelError(nameof(ConfirmPassword), "Mật khẩu xác nhận không khớp.");
                    return Page();
                }
                var (hash, salt) = _hasher.HashPassword(PlainPassword);
                Admin.passwordHashBase64 = hash;
                Admin.passwordSaltBase64 = salt;
            }

            _presenter ??= _presenterFactory(this);
            await _presenter.UpdateItemAsync(Admin);

            if (!string.IsNullOrEmpty(Error)) return Page();

            Message = "Cập nhật quản trị viên thành công.";
            return RedirectToPage("./AdminPage");
        }

        public void ShowItemDetail(AdminDto item)
        {
            Admin = item ?? new AdminDto();
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
            Response.Redirect(Url.Page("./AdminPage") ?? "/");
            return Task.CompletedTask;
        }
    }
}
