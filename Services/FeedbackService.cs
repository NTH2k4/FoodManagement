using FoodManagement.Contracts;
using FoodManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoodManagement.Services
{
    public class FeedbackService : IService<FeedbackDto>
    {
        private readonly IRepository<FeedbackDto> _repo;

        public FeedbackService(IRepository<FeedbackDto> repo) => _repo = repo;

        private static string NormalizeString(string? s) => string.IsNullOrWhiteSpace(s) ? string.Empty : s.Trim();

        public async Task<IEnumerable<FeedbackDto>> GetAllAsync() => await _repo.GetAllAsync();

        public async Task<FeedbackDto?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return await _repo.GetByIdAsync(id);
        }

        public async Task CreateAsync(FeedbackDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            dto.name = NormalizeString(dto.name);
            dto.phone = NormalizeString(dto.phone);
            dto.email = string.IsNullOrWhiteSpace(dto.email) ? null : NormalizeString(dto.email);
            dto.comment = string.IsNullOrWhiteSpace(dto.comment) ? null : NormalizeString(dto.comment);
            dto.createdAt = dto.createdAt == default ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : dto.createdAt;
            if (string.IsNullOrWhiteSpace(dto.id)) dto.id = Guid.NewGuid().ToString();
            await _repo.CreateAsync(dto);
        }

        public async Task UpdateAsync(FeedbackDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.id)) throw new ArgumentException("Id không hợp lệ.", nameof(dto));
            dto.name = NormalizeString(dto.name);
            dto.phone = NormalizeString(dto.phone);
            dto.email = string.IsNullOrWhiteSpace(dto.email) ? null : NormalizeString(dto.email);
            dto.comment = string.IsNullOrWhiteSpace(dto.comment) ? null : NormalizeString(dto.comment);
            await _repo.UpdateAsync(dto);
        }

        public async Task DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id không hợp lệ.", nameof(id));
            await _repo.DeleteAsync(id);
        }

        public async Task<(IEnumerable<FeedbackDto> Items, PaginationInfo Pagination)> QueryAsync(string? searchTerm, string? sortColumn, string? sortOrder, int page, int pageSize)
        {
            var all = (await _repo.GetAllAsync()) ?? Array.Empty<FeedbackDto>();
            IEnumerable<FeedbackDto> q = all;

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var t = searchTerm.Trim();
                q = q.Where(f =>
                    (!string.IsNullOrEmpty(f.name) && f.name.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(f.phone) && f.phone.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(f.email) && f.email.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(f.comment) && f.comment.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0)
                );
            }

            bool asc = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(sortColumn))
            {
                q = (sortColumn, asc) switch
                {
                    ("CustomerName", true) => q.OrderBy(f => f.name),
                    ("CustomerName", false) => q.OrderByDescending(f => f.name),
                    ("CreatedDate", true) => q.OrderBy(f => f.createdAt),
                    ("CreatedDate", false) => q.OrderByDescending(f => f.createdAt),
                    _ => q.OrderBy(f => f.id)
                };
            }
            else
            {
                q = q.OrderBy(f => f.id);
            }

            var totalItems = q.Count();
            if (pageSize <= 0) pageSize = 10;
            if (page <= 0) page = 1;
            var totalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalItems / pageSize) : 0;
            if (totalPages < 1) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var items = q.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var pagination = new PaginationInfo
            {
                TotalItems = totalItems,
                PageSize = pageSize,
                CurrentPage = page
            };

            return (items, pagination);
        }
    }
}
