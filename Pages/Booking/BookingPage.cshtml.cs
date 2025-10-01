using FoodManagement.Contracts;
using FoodManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoodManagement.Pages.Booking
{
    public class BookingPageModel : PageModel, IListView<BookingDto>
    {
        private readonly Func<IListView<BookingDto>, IPresenter<BookingDto>> _presenterFactory;
        private IPresenter<BookingDto>? _presenter;

        public BookingPageModel(Func<IListView<BookingDto>, IPresenter<BookingDto>> presenterFactory)
        {
            _presenterFactory = presenterFactory ?? throw new ArgumentNullException(nameof(presenterFactory));
        }

        public IEnumerable<BookingDto> Bookings { get; set; } = new List<BookingDto>();

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

        public void ShowItems(IEnumerable<BookingDto> items)
        {
            Bookings = items ?? Array.Empty<BookingDto>();
        }

        public void ShowItemDetail(BookingDto item)
        {
            ViewData["BookingDetail"] = item;
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
