using FoodManagement.Contracts;
using FoodManagement.Models;

namespace FoodManagement.Services
{
    public class AdminService : IService<AdminDto>
    {
        private readonly IAdminRepository _repo;
        public AdminService(IAdminRepository repo) => _repo = repo;
        public Task CreateAsync(AdminDto dto) => _repo.CreateAsync(dto);
        public Task DeleteAsync(string id) => _repo.DeleteAsync(id);
        public Task<IEnumerable<AdminDto>> GetAllAsync() => _repo.GetAllAsync();
        public Task<AdminDto?> GetByIdAsync(string id) => _repo.GetByIdAsync(id);
        public Task UpdateAsync(AdminDto dto) => _repo.UpdateAsync(dto);
    }
}
