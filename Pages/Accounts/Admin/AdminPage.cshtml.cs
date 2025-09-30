using FoodManagement.Contracts;
using FoodManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoodManagement.Pages.Accounts.Admin
{
    public class AdminPageModel : PageModel, IListView<AdminDto>
    {
        private readonly Func<IListView<AdminDto>, IPresenter<AdminDto>> _presenterFactory;
        private IPresenter<AdminDto>? _presenter;

        public AdminPageModel(Func<IListView<AdminDto>, IPresenter<AdminDto>> presenterFactory)
        {
            _presenterFactory = presenterFactory ?? throw new ArgumentNullException(nameof(presenterFactory));
        }

        [BindProperty]
        public AdminDto Input { get; set; } = new AdminDto();

        public IEnumerable<AdminDto> Admins { get; set; } = new List<AdminDto>();

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 20;

        [BindProperty(SupportsGet = true)]
        public int Page { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public string? SortColumn { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortOrder { get; set; }

        public PaginationInfo Pagination { get; set; } = new();

        [BindProperty]
        public string? DeleteId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Id { get; set; }

        public AdminDto? SelectedAdmin { get; set; }
        public bool ShowAdminInfo { get; set; }

        public async Task OnGetAsync()
        {
            _presenter ??= _presenterFactory(this);
            await _presenter.LoadItemsAsync(SearchTerm, SortColumn, SortOrder, Page, PageSize);
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            _presenter ??= _presenterFactory(this);
            await _presenter.CreateItemAsync(Input);
            if (!string.IsNullOrEmpty(ErrorMessage)) return Page();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            _presenter ??= _presenterFactory(this);
            await _presenter.UpdateItemAsync(Input);
            if (!string.IsNullOrEmpty(ErrorMessage)) return Page();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync([FromForm] string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                ErrorMessage = "ID không hợp lệ.";
                return RedirectToPage(new { pages = Page, PageSize = PageSize, SortColumn = SortColumn, SortOrder = SortOrder, SearchTerm = SearchTerm });
            }
            _presenter ??= _presenterFactory(this);
            await _presenter.DeleteItemAsync(id);
            if (!string.IsNullOrEmpty(ErrorMessage)) return RedirectToPage();
            StatusMessage = "Xóa admin thành công.";
            return RedirectToPage(new { pages = Page, PageSize = PageSize, SortColumn = SortColumn, SortOrder = SortOrder, SearchTerm = SearchTerm });
        }

        public void ShowItems(IEnumerable<AdminDto> items)
        {
            Admins = items ?? Array.Empty<AdminDto>();
        }

        public void ShowItemDetail(AdminDto item)
        {
            Input = item ?? new AdminDto();
        }

        public void ShowMessage(string message)
        {
            StatusMessage = message;
        }

        public void ShowError(string error)
        {
            ErrorMessage = error;
        }

        public void SetPagination(PaginationInfo pagination)
        {
            Pagination = pagination ?? new PaginationInfo();
        }

        public override void OnPageHandlerExecuting(Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext context)
        {
            base.OnPageHandlerExecuting(context);
        }

        public override void OnPageHandlerExecuted(Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutedContext context)
        {
            base.OnPageHandlerExecuted(context);
        }

        public async Task OnPostStartRealtimeAsync()
        {
            _presenter ??= _presenterFactory(this);
            await _presenter.LoadItemsAsync(SearchTerm, SortColumn, SortOrder, Page, PageSize);
        }

        public async Task OnPostStopRealtimeAsync()
        {
            _presenter ??= _presenterFactory(this);
            await _presenter.StopRealtimeAsync();
        }
    }
}
