using FoodManagement.Contracts;
using FoodManagement.Models;

namespace FoodManagement.Services
{
    public class BookingService : IService<BookingDto>
    {
        private readonly IRepository<BookingDto> _repo;
        public BookingService(IRepository<BookingDto> repo) => _repo = repo;

        public Task CreateAsync(BookingDto dto) => _repo.CreateAsync(dto);
        public Task DeleteAsync(string id) => _repo.DeleteAsync(id);
        public Task<IEnumerable<BookingDto>> GetAllAsync() => _repo.GetAllAsync();
        public Task<BookingDto?> GetByIdAsync(string id) => _repo.GetByIdAsync(id);
        public Task UpdateAsync(BookingDto dto) => _repo.UpdateAsync(dto);
    }
}
