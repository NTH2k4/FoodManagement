using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FoodManagement.Contracts;
using FoodManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodManagement.Pages.Login
{
    public class AccountModel : PageModel
    {
        private readonly IService<AdminDto> _service;

        public AccountModel(IService<AdminDto> service)
        {
            _service = service;
        }

        public AdminDto? Admin { get; private set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = GetUserIdFromClaims();
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Login/Login");
            }

            Admin = await _service.GetByIdAsync(userId);
            if (Admin == null) return RedirectToPage("/Login/Login");

            return Page();
        }

        private string? GetUserIdFromClaims()
        {
            // try common claim types
            var cid = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(cid)) return cid;

            cid = User?.FindFirst("id")?.Value;
            if (!string.IsNullOrWhiteSpace(cid)) return cid;

            cid = User?.FindFirst("sub")?.Value;
            if (!string.IsNullOrWhiteSpace(cid)) return cid;

            // fallback to username - not ideal but better than nothing
            var name = User?.Identity?.Name;
            return name;
        }
    }
}
