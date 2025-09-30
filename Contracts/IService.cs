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
        Task<(IEnumerable<T> Items, PaginationInfo Pagination)> QueryAsync(string? searchTerm, string? sortColumn, string? sortOrder, int page, int pageSize);
    }
}
