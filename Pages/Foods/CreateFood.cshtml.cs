using FoodManagement.Contracts;
using FoodManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FoodManagement.Pages.Foods
{
    public class CreateFoodModel : PageModel, ICreateView
    {
        private readonly Func<ICreateView, IPresenter<FoodDto>> _presenterFactory;

        public CreateFoodModel(Func<ICreateView, IPresenter<FoodDto>> presenterFactory)
        {
            _presenterFactory = presenterFactory ?? throw new ArgumentNullException(nameof(presenterFactory));
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
                Error = "Vui lòng kiểm tra lại thông tin.";
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

            var presenter = _presenterFactory(this);
            try
            {
                await presenter.CreateItemAsync(Food);
                if (!string.IsNullOrEmpty(Error)) return Page();
                Message = "Tạo món ăn thành công.";
                return RedirectToPage("./FoodPage");
            }
            catch (Exception ex)
            {
                Error = $"Lỗi khi tạo món ăn: {ex.Message}";
                return Page();
            }
        }

        public void ShowMessage(string message)
        {
            Message = message;
        }

        public void ShowError(string error)
        {
            Error = error;
        }

        public void ShowValidationErrors(System.Collections.Generic.IDictionary<string, string> fieldErrors)
        {
            if (fieldErrors == null) return;
            foreach (var kv in fieldErrors)
            {
                ModelState.AddModelError(kv.Key ?? string.Empty, kv.Value ?? string.Empty);
            }
        }

        public Task RedirectToListAsync()
        {
            Response.Redirect(Url.Page("./FoodPage") ?? "/");
            return Task.CompletedTask;
        }
    }
}
