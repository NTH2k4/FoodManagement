using FoodManagement.Contracts;
using FoodManagement.Models;
using FoodManagement.Presenters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodManagement.Pages.Accounts.Admin
{
    public class AdminPageModel : PageModel, IListView<AdminDto>
    {
        private readonly IService<AdminDto> _service;
        private IPresenter<AdminDto>? _presenter;
        private IEnumerable<AdminDto> _allAdmins = new List<AdminDto>();

        public IEnumerable<AdminDto> Admins { get; set; } = new List<AdminDto>();

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
        public string? SearchTerm { get; set; }

        public PaginationInfo Pagination { get; set; } = new();

        [BindProperty]
        public string? DeleteId { get; set; }

        public AdminDto? SelectedAdmin { get; set; }
        public bool ShowAdminInfo { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Id { get; set; }

        public AdminPageModel(IService<AdminDto> service)
        {
            _service = service;
        }

        public async Task OnGetAsync()
        {
            _presenter = new AdminPresenter(_service, this);
            await _presenter.LoadItemsAsync();

            var items = _allAdmins;

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var q = SearchTerm.Trim();
                items = items.Where(a =>
                    (!string.IsNullOrEmpty(a.firstName) && a.firstName.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(a.lastName) && a.lastName.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(a.username) && a.username.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(a.email) && a.email.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(a.phone) && a.phone.Contains(q, StringComparison.OrdinalIgnoreCase))
                );
            }

            if (!string.IsNullOrEmpty(SortColumn) && !string.IsNullOrEmpty(SortOrder))
            {
                items = SortAdmins(items, SortColumn, SortOrder);
            }

            var totalItems = items.Count();
            var totalPages = PageSize > 0 ? (int)Math.Ceiling((double)totalItems / PageSize) : 0;
            if (Pages < 1) Pages = 1;
            if (Pages > totalPages && totalPages > 0) Pages = totalPages;

            Pagination = new PaginationInfo
            {
                TotalItems = totalItems,
                PageSize = PageSize,
                CurrentPage = Pages
            };

            Admins = items.Skip((Pages - 1) * PageSize).Take(PageSize).ToList();

            if (!string.IsNullOrEmpty(Id))
            {
                SelectedAdmin = Admins.FirstOrDefault(a => a.id == Id);
                ShowAdminInfo = SelectedAdmin != null;
            }
        }

        private IEnumerable<AdminDto> SortAdmins(IEnumerable<AdminDto> items, string sortColumn, string sortOrder)
        {
            bool asc = sortOrder == "asc";
            return sortColumn switch
            {
                // full name = lastName + " " + firstName -> sort by lastName then firstName
                "fullname" => asc
                    ? items.OrderBy(a => a.lastName ?? string.Empty).ThenBy(a => a.firstName ?? string.Empty)
                    : items.OrderByDescending(a => a.lastName ?? string.Empty).ThenByDescending(a => a.firstName ?? string.Empty),
                "firstName" => asc ? items.OrderBy(a => a.firstName) : items.OrderByDescending(a => a.firstName),
                "lastName" => asc ? items.OrderBy(a => a.lastName) : items.OrderByDescending(a => a.lastName),
                "username" => asc ? items.OrderBy(a => a.username) : items.OrderByDescending(a => a.username),
                "email" => asc ? items.OrderBy(a => a.email) : items.OrderByDescending(a => a.email),
                "phone" => asc ? items.OrderBy(a => a.phone) : items.OrderByDescending(a => a.phone),
                "role" => asc ? items.OrderBy(a => (int)a.role) : items.OrderByDescending(a => (int)a.role),
                "LastLogin" => asc ? items.OrderBy(a => a.LastLoginAt) : items.OrderByDescending(a => a.LastLoginAt),
                "isActive" => asc ? items.OrderBy(a => a.isActive) : items.OrderByDescending(a => a.isActive),
                "CreatedDate" => asc ? items.OrderBy(a => a.createdAt) : items.OrderByDescending(a => a.createdAt),
                _ => items
            };
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            if (string.IsNullOrEmpty(DeleteId))
            {
                Error = "ID admin không hợp lệ.";
            }
            else
            {
                try
                {
                    _presenter = new AdminPresenter(_service, this);
                    await _presenter.DeleteItemAsync(DeleteId);
                }
                catch (Exception ex)
                {
                    Error = $"Lỗi khi xóa: {ex.Message}";
                }
            }

            return RedirectToPage(new
            {
                pages = Pages,
                PageSize = PageSize,
                SortColumn = SortColumn,
                SortOrder = SortOrder,
                SearchTerm = SearchTerm
            });
        }

        public void ShowItems(IEnumerable<AdminDto> items)
        {
            _allAdmins = items ?? Enumerable.Empty<AdminDto>();
        }

        public void ShowItemDetail(AdminDto item)
        {
            ViewData["AdminDetail"] = item;
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
