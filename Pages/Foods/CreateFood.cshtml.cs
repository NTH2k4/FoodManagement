using FoodManagement.Contracts;
using FoodManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodManagement.Pages.Foods
{
    public class CreateFoodModel : PageModel
    {
        private readonly IService<FoodDto> _service;

        public CreateFoodModel(IService<FoodDto> service)
        {
            _service = service;
        }

        [BindProperty]
        public FoodDto Food { get; set; } = new();

        [BindProperty]
        public string? ExtraImageUrls { get; set; }

        [TempData]
        public string? Message { get; set; }

        [TempData]
        public string? Error { get; set; }

        public void OnGet()
        {
            Food.price = 0;
            Food.sale = 0;
            Food.popular = false;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (!string.IsNullOrWhiteSpace(ExtraImageUrls))
            {
                var urls = ExtraImageUrls
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                Food.Images = urls
                    .Select((u, idx) => new ImageDto { id = idx + 1, url = u })
                    .ToList();
            }

            try
            {
                await _service.CreateAsync(Food);
                Message = "Tạo món ăn thành công.";
                return RedirectToPage("./FoodPage");
            }
            catch (Exception ex)
            {
                Error = $"Lỗi khi tạo món ăn: {ex.Message}";
                return Page();
            }
        }
    }
}
