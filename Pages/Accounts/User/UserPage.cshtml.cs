using FoodManagement.Contracts;
using FoodManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoodManagement.Pages.Accounts.User
{
    public class UserPageModel : PageModel, IListView<UserDto>
    {
        private readonly Func<IListView<UserDto>, IPresenter<UserDto>> _presenterFactory;
        private IPresenter<UserDto>? _presenter;

        public UserPageModel(Func<IListView<UserDto>, IPresenter<UserDto>> presenterFactory)
        {
            _presenterFactory = presenterFactory ?? throw new ArgumentNullException(nameof(presenterFactory));
        }

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
        public string? SearchTerm { get; set; }

        public PaginationInfo Pagination { get; set; } = new();

        [BindProperty]
        public string? DeleteId { get; set; }

        public UserDto? SelectedUser { get; set; }
        public bool ShowUserInfo { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Id { get; set; }

        public async Task OnGetAsync()
        {
            _presenter = _presenterFactory(this);
            await _presenter.LoadItemsAsync(SearchTerm, SortColumn, SortOrder, Pages, PageSize);
            if (!string.IsNullOrEmpty(Id))
            {
                SelectedUser = null;
                foreach (var u in Users)
                {
                    if (u.id == Id)
                    {
                        SelectedUser = u;
                        break;
                    }
                }
                ShowUserInfo = SelectedUser != null;
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            if (string.IsNullOrEmpty(DeleteId))
            {
                Error = "ID người dùng không hợp lệ.";
            }
            else
            {
                _presenter ??= _presenterFactory(this);
                await _presenter.DeleteItemAsync(DeleteId);
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

        public void ShowItems(IEnumerable<UserDto> items)
        {
            Users = items ?? Array.Empty<UserDto>();
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

        public void SetPagination(PaginationInfo pagination)
        {
            Pagination = pagination ?? new PaginationInfo();
        }
    }
}
