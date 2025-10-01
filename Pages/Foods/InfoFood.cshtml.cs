using FoodManagement.Contracts;
using FoodManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Threading.Tasks;

namespace FoodManagement.Pages.Foods
{
    public class InfoFoodModel : PageModel, IListView<FoodDto>
    {
        private readonly Func<IListView<FoodDto>, IPresenter<FoodDto>> _presenterFactory;
        private IPresenter<FoodDto>? _presenter;

        public InfoFoodModel(Func<IListView<FoodDto>, IPresenter<FoodDto>> presenterFactory)
        {
            _presenterFactory = presenterFactory ?? throw new ArgumentNullException(nameof(presenterFactory));
        }

        [BindProperty(SupportsGet = true)]
        public string? Id { get; set; }

        public FoodDto? Food { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return Page();
            _presenter ??= _presenterFactory(this);
            await _presenter.LoadItemByIdAsync(id);
            return Page();
        }

        public void ShowItems(System.Collections.Generic.IEnumerable<FoodDto> items) { }

        public void ShowItemDetail(FoodDto item)
        {
            Food = item;
        }

        public void ShowMessage(string message) { }

        public void ShowError(string error)
        {
            TempData["Error"] = error;
        }

        public void SetPagination(PaginationInfo pagination) { }
    }
}
