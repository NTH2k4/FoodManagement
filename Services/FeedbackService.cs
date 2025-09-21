using FoodManagement.Contracts;
using FoodManagement.Models;

namespace FoodManagement.Services
{
    public class FeedbackService : IService<FeedbackDto>
    {
        private readonly IRepository<FeedbackDto> _repo;
        public FeedbackService(IRepository<FeedbackDto> repo) => _repo = repo;
        public Task CreateAsync(FeedbackDto dto) => _repo.CreateAsync(dto);
        public Task DeleteAsync(string id) => _repo.DeleteAsync(id);
        public Task<IEnumerable<FeedbackDto>> GetAllAsync() => _repo.GetAllAsync();
        public Task<FeedbackDto?> GetByIdAsync(string id) => _repo.GetByIdAsync(id);
        public Task UpdateAsync(FeedbackDto dto) => _repo.UpdateAsync(dto);
    }
}
