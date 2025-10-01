using FoodManagement.Contracts;
using FoodManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoodManagement.Pages.Accounts.User
{
    public class EditUserModel : PageModel, IEditView<UserDto>
    {
        private readonly Func<IEditView<UserDto>, IPresenter<UserDto>> _presenterFactory;
        private IPresenter<UserDto>? _presenter;

        public EditUserModel(Func<IEditView<UserDto>, IPresenter<UserDto>> presenterFactory)
        {
            _presenterFactory = presenterFactory ?? throw new ArgumentNullException(nameof(presenterFactory));
        }

        [BindProperty(SupportsGet = true)]
        public string? Id { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Pages { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        [BindProperty(SupportsGet = true)]
        public string? SortColumn { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortOrder { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty]
        public UserDto User { get; set; } = new();

        [TempData]
        public string? Message { get; set; }

        [TempData]
        public string? Error { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Id))
            {
                Error = "Id không hợp lệ.";
                return Page();
            }
            _presenter ??= _presenterFactory(this);
            await _presenter.LoadItemByIdAsync(Id);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Id))
            {
                Error = "Id người dùng không hợp lệ.";
                return Page();
            }
            User.id = Id;
            if (!ModelState.IsValid) return Page();
            _presenter ??= _presenterFactory(this);
            try
            {
                await _presenter.UpdateItemAsync(User);
                if (!string.IsNullOrEmpty(Error)) return Page();
                Message = "Cập nhật tài khoản thành công.";
                return RedirectToPage("./UserPage", new
                {
                    pages = Pages,
                    PageSize = PageSize,
                    SortColumn = SortColumn,
                    SortOrder = SortOrder,
                    SearchTerm = SearchTerm
                });
            }
            catch (Exception ex)
            {
                Error = $"Lỗi khi lưu: {ex.Message}";
                return Page();
            }
        }

        public void ShowItemDetail(UserDto item)
        {
            User = item ?? new UserDto();
            Id = User.id;
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
            Response.Redirect(Url.Page("./UserPage") ?? "/");
            return Task.CompletedTask;
        }
    }
}
