using FoodManagement.Contracts;
using FoodManagement.Presenters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace FoodManagement.Pages.Login
{
    [AllowAnonymous]
    public class LoginModel : PageModel, ILoginView
    {
        private readonly LoginPresenter _presenter;

        public LoginModel(IAdminRepository repo, IPasswordHasher hasher, IAuthService auth, IAuditService audit)
        {
            _presenter = new LoginPresenter(this, repo, hasher, auth, audit);
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? Message { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
            public string Username { get; set; } = string.Empty;
            [Required(ErrorMessage = "Mật khẩu không được để trống.")]
            public string Password { get; set; } = string.Empty;
            public bool RememberMe { get; set; }
        }

        string ILoginView.Username => Input.Username;
        string ILoginView.Password => Input.Password;
        bool ILoginView.RememberMe => Input.RememberMe;
        void ILoginView.ShowError(string message) => Message = message;
        void ILoginView.RedirectTo(string url) => Response.Redirect(url);

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            await _presenter.HandleLoginAsync();
            return Page();
        }
    }
}
