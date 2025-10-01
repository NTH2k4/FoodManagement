// Pages/Feedbacks/FeedbackPageModel.cs
using FoodManagement.Contracts;
using FoodManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoodManagement.Pages.Feedbacks
{
    public class FeedbackPageModel : PageModel, IListView<FeedbackDto>
    {
        private readonly Func<IListView<FeedbackDto>, IPresenter<FeedbackDto>> _presenterFactory;
        private IPresenter<FeedbackDto>? _presenter;

        public FeedbackPageModel(Func<IListView<FeedbackDto>, IPresenter<FeedbackDto>> presenterFactory)
        {
            _presenterFactory = presenterFactory ?? throw new ArgumentNullException(nameof(presenterFactory));
        }

        public IEnumerable<FeedbackDto> Feedbacks { get; set; } = new List<FeedbackDto>();

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

        public async Task OnGetAsync()
        {
            _presenter = _presenterFactory(this);
            await _presenter.LoadItemsAsync(SearchTerm, SortColumn, SortOrder, Pages, PageSize);
        }

        public void ShowItems(IEnumerable<FeedbackDto> items)
        {
            Feedbacks = items ?? Array.Empty<FeedbackDto>();
        }

        public void ShowItemDetail(FeedbackDto item)
        {
            ViewData["FeedbackDetail"] = item;
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
