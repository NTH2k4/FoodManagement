using FoodManagement.Models;

namespace FoodManagement.Contracts.Foods
{
    public interface IFoodRepository
    {
        Task<IEnumerable<FoodDto>> GetAllAsync(CancellationToken ct = default);
        Task<FoodDto?> GetByIdAsync(string id, CancellationToken ct = default);
        Task CreateAsync(FoodDto dto, CancellationToken ct = default);
        Task UpdateAsync(FoodDto dto, CancellationToken ct = default);
        Task DeleteAsync(string id, CancellationToken ct = default);
    }
}
