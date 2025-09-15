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

        // Dùng để nhận nhiều link ảnh phụ (mỗi dòng 01 URL)
        [BindProperty]
        public string? ExtraImageUrls { get; set; }

        [TempData]
        public string? Message { get; set; }

        [TempData]
        public string? Error { get; set; }

        public void OnGet()
        {
            // Giá trị mặc định khi mở trang
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

            // chuyển các URL ảnh phụ thành ImageDto list (nếu có)
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
                // Gọi service tạo mới
                await _service.CreateAsync(Food);

                // Dùng PRG: chuyển hướng về danh sách sau khi tạo để tránh form resubmission
                Message = "Tạo món ăn thành công.";
                return RedirectToPage("./FoodPage");
            }
            catch (Exception ex)
            {
                // hiện lỗi ngay trên trang (không redirect)
                Error = $"Lỗi khi tạo món ăn: {ex.Message}";
                return Page();
            }
        }
    }
}
