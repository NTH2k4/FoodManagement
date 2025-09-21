using FoodManagement.Contracts;
using FoodManagement.Models;
using FoodManagement.Presenters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodManagement.Pages.Feedbacks
{
    public class FeedbackPageModel : PageModel, IListView<FeedbackDto>
    {
        private readonly IService<FeedbackDto> _service;
        private IPresenter<FeedbackDto>? _presenter;
        private IEnumerable<FeedbackDto> _allFeedbacks = new List<FeedbackDto>();
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
        public string? SearchTerm { get; set; } // Thêm thuộc tính tìm kiếm
        public PaginationInfo Pagination { get; set; } = new();

        public FeedbackPageModel(IService<FeedbackDto> service)
        {
            _service = service;
        }

        public async Task OnGetAsync()
        {
            _presenter = new FeedbackPresenter(_service, this);
            await _presenter.LoadItemsAsync();
            var feedbacks = _allFeedbacks;
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                feedbacks = feedbacks.Where(b => b.name != null && b.name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) || b.phone != null && b.phone.Contains(SearchTerm));
            }
            // Sorting
            feedbacks = (SortColumn, SortOrder) switch
            {
                ("CustomerName", "asc") => feedbacks.OrderBy(b => b.name),
                ("CustomerName", "desc") => feedbacks.OrderByDescending(b => b.name),
                ("CreatedDate", "asc") => feedbacks.OrderBy(b => b.createdAt),
                ("CreatedDate", "desc") => feedbacks.OrderByDescending(b => b.createdAt),
                _ => feedbacks.OrderBy(b => b.id),
            };
            // Pagination
            var count = feedbacks.Count();
            var totalPages = (int)Math.Ceiling(count / (double)PageSize);
            if (Pages < 1) Pages = 1;
            else if (Pages > totalPages) Pages = totalPages;

            Pagination = new PaginationInfo
            {
                CurrentPage = Pages,
                PageSize = PageSize,
                TotalItems = count,
            };
            Feedbacks = feedbacks.Skip((Pages - 1) * PageSize).Take(PageSize).ToList();
        }

        // ===============================
        // Implementation of IListView
        // ===============================
        public void ShowItems(IEnumerable<FeedbackDto> items)
        {
            _allFeedbacks = items;
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
    }
}
