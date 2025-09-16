using FoodManagement.Contracts;
using FoodManagement.Models;

namespace FoodManagement.Services
{
    public class UserService : IService<UserDto>
    {
        private readonly IRepository<UserDto> _repo;
        public UserService(IRepository<UserDto> repo) => _repo = repo;

        public Task CreateAsync(UserDto dto) => _repo.CreateAsync(dto);
        public Task DeleteAsync(string id) => _repo.DeleteAsync(id);
        public Task<IEnumerable<UserDto>> GetAllAsync() => _repo.GetAllAsync();
        public Task<UserDto?> GetByIdAsync(string id) => _repo.GetByIdAsync(id);
        public Task UpdateAsync(UserDto dto) => _repo.UpdateAsync(dto);
    }
}
