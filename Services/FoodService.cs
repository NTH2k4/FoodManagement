using FoodManagement.Contracts;
using FoodManagement.Models;

namespace FoodManagement.Services
{
    public class FoodService : IService<FoodDto>
    {
        private readonly IRepository<FoodDto> _repo;
        public FoodService(IRepository<FoodDto> repo) => _repo = repo;

        public Task<IEnumerable<FoodDto>> GetAllAsync() => _repo.GetAllAsync();
        public Task<FoodDto?> GetByIdAsync(string id) => _repo.GetByIdAsync(id);
        public Task CreateAsync(FoodDto dto) => _repo.CreateAsync(dto);
        public Task UpdateAsync(FoodDto dto) => _repo.UpdateAsync(dto);
        public Task DeleteAsync(string id) => _repo.DeleteAsync(id);
    }
}
