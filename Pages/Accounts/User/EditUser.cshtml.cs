using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FoodManagement.Models;
using FoodManagement.Contracts;

namespace FoodManagement.Pages.Accounts.User
{
    public class EditUserModel : PageModel
    {
        private readonly IService<UserDto> _service;

        public EditUserModel(IService<UserDto> service)
        {
            _service = service;
        }

        // Id bind được từ route/query/hidden input (GET + POST)
        [BindProperty(SupportsGet = true)]
        public string? Id { get; set; }

        // preserve pagination/filter query params so we can return back with same state
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

        // Bind the editable fields (note: UserDto.id has [BindNever], so we don't expect it to be bound)
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

            try
            {
                var dto = await _service.GetByIdAsync(Id);
                if (dto == null)
                {
                    Error = "Không tìm thấy người dùng.";
                    return Page();
                }
                User = dto;
                Id = dto.id;
                return Page();
            }
            catch (Exception ex)
            {
                Error = $"Lỗi khi tải dữ liệu: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Id))
            {
                Error = "Id người dùng không hợp lệ.";
                return Page();
            }
            User.id = Id;
            try
            {
                if (string.IsNullOrEmpty(User.password))
                {
                    var existing = await _service.GetByIdAsync(Id);
                    if (existing != null)
                    {
                        User.password = existing.password;
                    }
                }

                if (!ModelState.IsValid)
                {
                    return Page();
                }
                await _service.UpdateAsync(User);
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
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("User.phone", ex.Message);
                return Page();
            }
            catch (Exception ex)
            {
                Error = $"Lỗi khi lưu: {ex.Message}";
                return Page();
            }
        }
    }
}
