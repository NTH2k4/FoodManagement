using FoodManagement.Models;

namespace FoodManagement.Contracts
{
    public interface IService<T>
    {
        Task<IEnumerable<FoodDto>> GetAllAsync();
        Task<FoodDto?> GetByIdAsync(string id);
        Task CreateAsync(T dto);
        Task UpdateAsync(T dto);
        Task DeleteAsync(string id);
    }
}
