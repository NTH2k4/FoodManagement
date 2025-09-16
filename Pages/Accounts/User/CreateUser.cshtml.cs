using FoodManagement.Contracts;
using FoodManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodManagement.Pages.Accounts.User
{
    public class CreateUserModel : PageModel
    {
        private readonly IService<UserDto> _service;
        public CreateUserModel(IService<UserDto> service)
        {
            _service = service;
        }

        [BindProperty]
        public UserDto User { get; set; } = new();

        [TempData]
        public string? Message { get; set; }
        [TempData]
        public string? Error { get; set; }

        public void OnGet()
        {
            
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                //foreach (var key in ModelState.Keys)
                //{
                //    var state = ModelState[key];
                //    if (state != null && state.Errors.Count > 0)
                //    {
                //        foreach (var error in state.Errors)
                //        {
                //            Console.WriteLine($"ModelState error for {key}: {error.ErrorMessage}");
                //        }
                //    }
                //}
                Error = "Vui lòng kiểm tra lại thông tin.";
                return Page();
            }
            try
            {
                User.id = Guid.NewGuid().ToString();
                User.createdAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 60;
                await _service.CreateAsync(User);
                Message = "Tạo tài khoản thành công.";
                return RedirectToPage("./UserPage");
            }
            catch (Exception ex)
            {
                Error = $"Lỗi khi tạo tài khoản: {ex.Message}";
                return Page();
            }
        }
    }
}
