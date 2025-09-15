using FoodManagement.Contracts.Foods;
using FoodManagement.Models;
using FoodManagement.Presenters.Foods;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodManagement.Pages.Foods
{
    public class FoodPageModel(IFoodService service) : PageModel, IFoodListView
    {
        private readonly IFoodService _service = service;
        private IFoodPresenter? _presenter;

        private IEnumerable<FoodDto> _allFoods = new List<FoodDto>();
        public IEnumerable<FoodDto> Foods { get; private set; } = new List<FoodDto>();
        public string? Message { get; private set; }
        public string? Error { get; private set; }

        [BindProperty(SupportsGet = true)]
        public int pages { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 5;

        [BindProperty(SupportsGet = true)]
        public string? SortColumn { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortOrder { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; } // Thêm thuộc tính tìm kiếm

        [BindProperty]
        public string? DeleteId { get; set; }

        public PaginationInfo Pagination { get; set; } = new();

        public async Task OnGetAsync()
        {
            _presenter = new FoodPresenter(_service, this);
            await _presenter.LoadFoodsAsync();

            var foods = _allFoods;
            // Lọc theo SearchTerm nếu có
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                foods = foods.Where(f => !string.IsNullOrEmpty(f.name) && f.name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrEmpty(SortColumn) && !string.IsNullOrEmpty(SortOrder))
            {
                foods = SortFoods(foods, SortColumn, SortOrder);
            }

            var totalItems = foods.Count();
            var totalPages = PageSize > 0 ? (int)Math.Ceiling((double)totalItems / PageSize) : 0;
            if (pages < 1) pages = 1;
            if (pages > totalPages && totalPages > 0) pages = totalPages;

            Pagination = new PaginationInfo
            {
                TotalItems = totalItems,
                PageSize = PageSize,
                CurrentPage = pages
            };
            Foods = foods.Skip((pages - 1) * PageSize).Take(PageSize).ToList();
        }

        private IEnumerable<FoodDto> SortFoods(IEnumerable<FoodDto> foods, string column, string order)
        {
            bool asc = order == "asc";
            return column switch
            {
                "name" => asc ? foods.OrderBy(f => f.name) : foods.OrderByDescending(f => f.name),
                "price" => asc ? foods.OrderBy(f => f.price) : foods.OrderByDescending(f => f.price),
                "sale" => asc ? foods.OrderBy(f => f.sale) : foods.OrderByDescending(f => f.sale),
                "popular" => asc ? foods.OrderBy(f => f.popular) : foods.OrderByDescending(f => f.popular),
                _ => foods
            };
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            if (string.IsNullOrEmpty(DeleteId))
            {
                Error = "Id không hợp lệ.";
            }
            else
            {
                try
                {
                    await _service.DeleteAsync(DeleteId);
                    Message = "Xóa món ăn thành công.";
                }
                catch (Exception ex)
                {
                    Error = $"Lỗi khi xóa: {ex.Message}";
                }
            }
            // reload data
            var foods = await _service.GetAllAsync();
            Foods = foods.ToList();

            return Page();
        }

        // ===============================
        // Implementation of IFoodListView
        // ===============================
        public void ShowFoods(IEnumerable<FoodDto> foods)
        {
            _allFoods = foods;
        }

        public void ShowFoodDetail(FoodDto food)
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
    }
}
