using FoodManagement.Contracts;
using FoodManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoodManagement.Pages.Foods
{
    public class EditModel : PageModel, IEditView<FoodDto>
    {
        private readonly Func<IEditView<FoodDto>, IPresenter<FoodDto>> _presenterFactory;
        private IPresenter<FoodDto>? _presenter;

        public EditModel(Func<IEditView<FoodDto>, IPresenter<FoodDto>> presenterFactory)
        {
            _presenterFactory = presenterFactory ?? throw new ArgumentNullException(nameof(presenterFactory));
        }

        [BindProperty]
        public FoodDto Food { get; set; } = new();

        [TempData]
        public string? Message { get; set; }

        [TempData]
        public string? Error { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            _presenter ??= _presenterFactory(this);
            await _presenter.LoadItemByIdAsync(id);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();
            _presenter ??= _presenterFactory(this);
            await _presenter.UpdateItemAsync(Food);
            if (!string.IsNullOrEmpty(Error)) return Page();
            Message = "Cập nhật món ăn thành công.";
            return RedirectToPage("/Foods/FoodPage");
        }

        public void ShowItemDetail(FoodDto item)
        {
            Food = item ?? new FoodDto();
        }

        public void ShowMessage(string message)
        {
            Message = message;
        }

        public void ShowError(string error)
        {
            Error = error;
        }

        public void ShowValidationErrors(IDictionary<string, string> fieldErrors)
        {
            if (fieldErrors == null) return;
            foreach (var kv in fieldErrors)
            {
                ModelState.AddModelError(kv.Key ?? string.Empty, kv.Value ?? string.Empty);
            }
        }

        public Task RedirectToListAsync()
        {
            Response.Redirect(Url.Page("/Foods/FoodPage") ?? "/");
            return Task.CompletedTask;
        }
    }
}
