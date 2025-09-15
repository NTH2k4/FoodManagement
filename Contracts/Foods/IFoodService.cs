using FoodManagement.Models;

namespace FoodManagement.Contracts.Foods
{
    public interface IFoodService
    {
        Task<IEnumerable<FoodDto>> GetAllAsync();
        Task<FoodDto?> GetByIdAsync(string id);
        Task CreateAsync(FoodDto dto);
        Task UpdateAsync(FoodDto dto);
        Task DeleteAsync(string id);
    }
}
