using FoodManagement.Models;
using System.Threading.Tasks;

namespace FoodManagement.Contracts
{
    public interface IAdminService : IService<AdminDto>
    {
        Task ChangePasswordAsync(string adminId, string currentPassword, string newPassword);
    }
}
