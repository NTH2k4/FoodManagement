using FoodManagement.Models;

namespace FoodManagement.Contracts
{
    public interface IRepository<T>
    {
        Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
        Task<T?> GetByIdAsync(string id, CancellationToken ct = default);
        Task CreateAsync(T dto, CancellationToken ct = default);
        Task UpdateAsync(T dto, CancellationToken ct = default);
        Task DeleteAsync(string id, CancellationToken ct = default);
    }
}
