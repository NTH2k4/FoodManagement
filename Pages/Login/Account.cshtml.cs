using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FoodManagement.Contracts;
using FoodManagement.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodManagement.Pages.Login
{
    public class AccountModel : PageModel, IListView<AdminDto>
    {
        private readonly Func<IListView<AdminDto>, IPresenter<AdminDto>> _presenterFactory;
        private IPresenter<AdminDto>? _presenter;

        public AccountModel(Func<IListView<AdminDto>, IPresenter<AdminDto>> presenterFactory)
        {
            _presenterFactory = presenterFactory ?? throw new ArgumentNullException(nameof(presenterFactory));
        }

        public AdminDto? Admin { get; private set; }
        public string? Message { get; private set; }

        public async Task OnGetAsync()
        {
            var userId = GetUserIdFromClaims();
            if (string.IsNullOrEmpty(userId))
            {
                Response.Redirect("/Login/Login");
                return;
            }
            _presenter ??= _presenterFactory(this);
            await _presenter.LoadItemByIdAsync(userId);
            Message = TempData["Message"] as string;
        }

        public void ShowItems(System.Collections.Generic.IEnumerable<AdminDto> items) { }

        public void ShowItemDetail(AdminDto item)
        {
            Admin = item;
        }

        public void ShowMessage(string message) { }

        public void ShowError(string error)
        {
            TempData["Error"] = error;
        }

        public void SetPagination(PaginationInfo pagination) { }

        private string? GetUserIdFromClaims()
        {
            var cid = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(cid)) return cid;
            cid = User?.FindFirst("id")?.Value;
            if (!string.IsNullOrWhiteSpace(cid)) return cid;
            cid = User?.FindFirst("sub")?.Value;
            if (!string.IsNullOrWhiteSpace(cid)) return cid;
            return User?.Identity?.Name;
        }
    }
}
