using FoodManagement.Models;

namespace FoodManagement.Contracts
{
    public interface IService<T>
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> GetByIdAsync(string id);
        Task CreateAsync(T dto);
        Task UpdateAsync(T dto);
        Task DeleteAsync(string id);
    }
}
