using FoodManagement.Contracts.Foods;
using FoodManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodManagement.Pages.Foods
{
    public class EditModel : PageModel
    {
        private readonly IFoodService _service;

        public EditModel(IFoodService service)
        {
            _service = service;
        }

        [BindProperty]
        public FoodDto Food { get; set; } = new();

        [TempData]
        public string? Message { get; set; }

        [TempData]
        public string? Error { get; set; }

        // Route expects id as route parameter /Foods/Edit/{id}
        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var dto = await _service.GetByIdAsync(id);
            if (dto == null)
                return NotFound();

            Food = dto;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            try
            {
                await _service.UpdateAsync(Food);
                Message = "Cập nhật món ăn thành công.";
                return RedirectToPage("/Foods/FoodPage");
            }
            catch (Exception ex)
            {
                // Log/handle as needed
                Error = $"Lỗi khi cập nhật: {ex.Message}";
                return Page();
            }
        }
    }
}
