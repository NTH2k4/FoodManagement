using FoodManagement.Models;

namespace FoodManagement.Contracts
{
    public interface IAdminRepository : IRepository<AdminDto>
    {
        Task<AdminDto?> GetByUsernameAsync(string username, CancellationToken ct = default);
    }
}
