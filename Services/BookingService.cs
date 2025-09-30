using FoodManagement.Contracts;
using FoodManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoodManagement.Services
{
    public class BookingService : IService<BookingDto>
    {
        private readonly IRepository<BookingDto> _repo;

        public BookingService(IRepository<BookingDto> repo) => _repo = repo;

        private static string NormalizeString(string? s) => string.IsNullOrWhiteSpace(s) ? string.Empty : s.Trim();

        public async Task<IEnumerable<BookingDto>> GetAllAsync() => await _repo.GetAllAsync();

        public async Task<BookingDto?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return await _repo.GetByIdAsync(id);
        }

        public async Task CreateAsync(BookingDto dto)
        {
            
        }

        public async Task UpdateAsync(BookingDto dto)
        {
            
        }

        public async Task DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id không hợp lệ.", nameof(id));
            await _repo.DeleteAsync(id);
        }

        public async Task<(IEnumerable<BookingDto> Items, PaginationInfo Pagination)> QueryAsync(string? searchTerm, string? sortColumn, string? sortOrder, int page, int pageSize)
        {
            var all = (await _repo.GetAllAsync()) ?? Array.Empty<BookingDto>();
            IEnumerable<BookingDto> q = all;

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var t = searchTerm.Trim();
                q = q.Where(b =>
                    (!string.IsNullOrEmpty(b.name) && b.name.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(b.phone) && b.phone.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(b.address) && b.address.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(b.foods) && b.foods.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0)
                );
            }

            bool asc = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(sortColumn))
            {
                q = (sortColumn, asc) switch
                {
                    ("CustomerName", true) => q.OrderBy(b => b.name),
                    ("CustomerName", false) => q.OrderByDescending(b => b.name),
                    ("CreatedDate", true) => q.OrderBy(b => b.createdAt),
                    ("CreatedDate", false) => q.OrderByDescending(b => b.createdAt),
                    _ => q.OrderBy(b => b.id)
                };
            }
            else
            {
                q = q.OrderBy(b => b.id);
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
