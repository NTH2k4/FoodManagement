using FoodManagement.Contracts;
using FoodManagement.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace FoodManagement.Security
{
    public class CookieAuthService : IAuthService
    {
        private readonly IHttpContextAccessor _http;

        public CookieAuthService(IHttpContextAccessor http) => _http = http;

        public async Task SignInAsync(AdminDto admin, bool isPersistent = false)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, admin.id),
                new Claim(ClaimTypes.Name, admin.username),
                new Claim(ClaimTypes.Role, admin.role == AdminRole.SuperAdmin ? "SuperAdmin" : "Staff"),
                new Claim("role", admin.role.ToString())
            };

            if (admin.role == AdminRole.SuperAdmin)
            {
                claims.Add(new Claim("permission", "Manage.Users"));
                claims.Add(new Claim("permission", "Manage.Settings"));
            }
            else
            {
                claims.Add(new Claim("permission", "Manage.Menu"));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var props = new AuthenticationProperties { IsPersistent = isPersistent };
            await _http.HttpContext!.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);
        }

        public Task SignOutAsync() => _http.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
