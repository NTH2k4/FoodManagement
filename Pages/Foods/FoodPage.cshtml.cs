using FoodManagement.Contracts;
using FoodManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoodManagement.Pages.Foods
{
    public class FoodPageModel : PageModel, IListView<FoodDto>
    {
        private readonly Func<IListView<FoodDto>, IPresenter<FoodDto>> _presenterFactory;
        private IPresenter<FoodDto>? _presenter;

        public FoodPageModel(Func<IListView<FoodDto>, IPresenter<FoodDto>> presenterFactory)
        {
            _presenterFactory = presenterFactory ?? throw new ArgumentNullException(nameof(presenterFactory));
        }

        private IEnumerable<FoodDto> _allFoods = new List<FoodDto>();
        public IEnumerable<FoodDto> Foods { get; private set; } = new List<FoodDto>();
        [TempData]
        public string? Message { get; set; }
        [TempData]
        public string? Error { get; set; }

        [BindProperty(SupportsGet = true)]
        public int pages { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        [BindProperty(SupportsGet = true)]
        public string? SortColumn { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortOrder { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty]
        public string? DeleteId { get; set; }

        public PaginationInfo Pagination { get; set; } = new();

        public async Task OnGetAsync()
        {
            _presenter = _presenterFactory(this);
            await _presenter.LoadItemsAsync(SearchTerm, SortColumn, SortOrder, pages, PageSize);
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            if (string.IsNullOrEmpty(DeleteId))
            {
                Error = "Id không hợp lệ.";
            }
            else
            {
                _presenter ??= _presenterFactory(this);
                await _presenter.DeleteItemAsync(DeleteId);
            }
            return RedirectToPage(new
            {
                pages = pages,
                PageSize = PageSize,
                SortColumn = SortColumn,
                SortOrder = SortOrder,
                SearchTerm = SearchTerm
            });
        }

        public void ShowItems(IEnumerable<FoodDto> foods)
        {
            _allFoods = foods ?? Array.Empty<FoodDto>();
            Foods = _allFoods;
        }

        public void ShowItemDetail(FoodDto food)
        {
            ViewData["FoodDetail"] = food;
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
