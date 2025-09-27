using FoodManagement.Models;

namespace FoodManagement.Contracts
{
    public interface IAuthService
    {
        Task SignInAsync(AdminDto admin, bool isPersistent = false);
        Task SignOutAsync();
    }
}
