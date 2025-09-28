using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace FoodManagement.Pages.Login
{
    public class LogoutModel : PageModel
    {
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(ILogger<LogoutModel> logger)
        {
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            return RedirectToPage("/Login/Login");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                var sessionFeature = HttpContext.Features.Get<ISessionFeature>();
                if (sessionFeature?.Session != null)
                {
                    try
                    {
                        sessionFeature.Session.Clear();
                    }
                    catch {}
                }

                _logger.LogInformation("User logged out.");

                return RedirectToPage("/Login/Login");
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return RedirectToPage("/Login/Login");
            }
        }
    }
}
