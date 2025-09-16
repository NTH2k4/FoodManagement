using FoodManagement.Contracts;
using FoodManagement.Models;
using FoodManagement.Presenters.Foods;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;

namespace FoodManagement.Pages.Accounts.User
{
    public class UserPageModel : PageModel, IListView<UserDto>
    {
        private readonly IService<UserDto> _service;
        private IPresenter<UserDto>? _presenter;
        private IEnumerable<UserDto> _allUsers = new List<UserDto>();
        public IEnumerable<UserDto> Users { get; set; } = new List<UserDto>();
        [TempData]
        public string? Message { get; set; }
        [TempData]
        public string? Error { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Pages { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        [BindProperty(SupportsGet = true)]
        public string? SortColumn { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortOrder { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; } // Thêm thuộc tính tìm kiếm

        public PaginationInfo Pagination { get; set; } = new();

        [BindProperty]
        public string? DeleteId { get; set; }

        public UserDto? SelectedUser { get; set; }
        public bool ShowUserInfo { get; set; }

        public UserPageModel(IService<UserDto> service)
        {
            _service = service;
        }

        // Add this property to bind the "id" query parameter from the URL
        [BindProperty(SupportsGet = true)]
        public string? Id { get; set; }

        // Replace the usage of "id" with "Id" in OnGetAsync
        public async Task OnGetAsync()
        {
            _presenter = new UserPresenter(_service, this);
            await _presenter.LoadItemsAsync();
            var users = _allUsers;
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                users = users.Where(u => !string.IsNullOrEmpty(u.fullName) && u.fullName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrEmpty(SortColumn) && !string.IsNullOrEmpty(SortOrder))
            {
                users = SortUsers(users, SortColumn, SortOrder);
            }

            var totalItems = users.Count();
            var totalPages = PageSize > 0 ? (int)Math.Ceiling((double)totalItems / PageSize) : 0;
            if (Pages < 1) Pages = 1;
            if (Pages > totalPages && totalPages > 0) Pages = totalPages;

            Pagination = new PaginationInfo
            {
                TotalItems = totalItems,
                PageSize = PageSize,
                CurrentPage = Pages
            };
            Users = users.Skip((Pages - 1) * PageSize).Take(PageSize).ToList();

            if (!string.IsNullOrEmpty(Id))
            {
                SelectedUser = Users.FirstOrDefault(u => u.id == Id);
                ShowUserInfo = SelectedUser != null;
            }
        }

        private IEnumerable<UserDto> SortUsers(IEnumerable<UserDto> users, string sortColumn, string sortOrder)
        {
            bool asc = sortOrder == "asc";
            return sortColumn switch
            {
                "fullname" => asc ? users.OrderBy(u => u.fullName) : users.OrderByDescending(u => u.fullName),
                "email" => asc ? users.OrderBy(u => u.email) : users.OrderByDescending(u => u.email),
                _ => users
            };
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            if (string.IsNullOrEmpty(DeleteId))
            {
                Error = "ID người dùng không hợp lệ.";
            }
            else
            {
                try
                {
                    await _service.DeleteAsync(DeleteId);
                    Message = "Xóa người dùng thành công.";
                }
                catch (Exception ex)
                {
                    Error = $"Lỗi khi xóa: {ex.Message}";
                }
            }
            return RedirectToPage(new {
                pages = Pages,
                PageSize = PageSize,
                SortColumn = SortColumn,
                SortOrder = SortOrder,
                SearchTerm = SearchTerm
            });
        }

        // ===============================
        // Implementation of IListView
        // ===============================
        public void ShowItems(IEnumerable<UserDto> items)
        {
            _allUsers = items;
        }

        public void ShowItemDetail(UserDto item)
        {
            ViewData["UserDetail"] = item;
        }

        public void ShowMessage(string message)
        {
            Message = message;
        }

        public void ShowError(string error)
        {
            Error = error;
        }

    }
}
