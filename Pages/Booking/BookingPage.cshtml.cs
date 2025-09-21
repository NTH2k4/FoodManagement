using FoodManagement.Contracts;
using FoodManagement.Models;
using FoodManagement.Presenters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodManagement.Pages.Booking
{
    public class BookingPageModel : PageModel, IListView<BookingDto>
    {
        private readonly IService<BookingDto> _service;
        private IPresenter<BookingDto>? _presenter;
        private IEnumerable<BookingDto> _allBookings = new List<BookingDto>();
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
        public string? SearchTerm { get; set; } // Thêm thuộc tính tìm kiếm
        public PaginationInfo Pagination { get; set; } = new();

        public BookingPageModel(IService<BookingDto> service)
        {
            _service = service;
        }

        public async Task OnGetAsync()
        {
            _presenter = new BookingPresenter(_service, this);
            await _presenter.LoadItemsAsync();
            var bookings = _allBookings;
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                bookings = bookings.Where(b => b.name != null && b.name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) || b.phone != null && b.phone.Contains(SearchTerm));
            }
            // Sorting
            bookings = (SortColumn, SortOrder) switch
            {
                ("CustomerName", "asc") => bookings.OrderBy(b => b.name),
                ("CustomerName", "desc") => bookings.OrderByDescending(b => b.name),
                ("CreatedDate", "asc") => bookings.OrderBy(b => b.createdAt),
                ("CreatedDate", "desc") => bookings.OrderByDescending(b => b.createdAt),
                _ => bookings.OrderBy(b => b.id),
            };
            // Pagination
            var totalItems = bookings.Count();
            var totalPages = PageSize > 0 ? (int)Math.Ceiling((double)totalItems / PageSize) : 0;
            if (Pages < 1) Pages = 1;
            if (Pages > totalPages && totalPages > 0) Pages = totalPages;

            Pagination = new PaginationInfo
            {
                CurrentPage = Pages,
                PageSize = PageSize,
                TotalItems = totalItems
            };
            Bookings = bookings.Skip((Pages - 1) * PageSize).Take(PageSize).ToList();

            if (!string.IsNullOrEmpty(SortColumn) && !string.IsNullOrEmpty(SortOrder))
            {
                SortOrder = SortOrder == "asc" ? "desc" : "asc";
            }
            else
            {
                SortOrder = "asc";
            }
        }

        // ===============================
        // Implementation of IListView
        // ===============================
        public void ShowItems(IEnumerable<BookingDto> items)
        {
            _allBookings = items;
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
    }
}
