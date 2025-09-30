using FoodManagement.Contracts;
using FoodManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoodManagement.Services
{
    public class FoodService : IService<FoodDto>
    {
        private readonly IRepository<FoodDto> _repo;

        public FoodService(IRepository<FoodDto> repo) => _repo = repo;

        private static string NormalizeString(string? s) => string.IsNullOrWhiteSpace(s) ? string.Empty : s.Trim();

        public async Task<IEnumerable<FoodDto>> GetAllAsync() => await _repo.GetAllAsync();

        public async Task<FoodDto?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return await _repo.GetByIdAsync(id);
        }

        public async Task CreateAsync(FoodDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            dto.name = NormalizeString(dto.name);
            dto.description = string.IsNullOrWhiteSpace(dto.description) ? null : NormalizeString(dto.description);
            dto.Images = dto.Images ?? new List<ImageDto>();
            dto.price = dto.price <= 0 ? 0 : dto.price;
            dto.sale = dto.sale < 0 ? 0 : dto.sale;
            dto.popular = dto.popular;
            //if (string.IsNullOrWhiteSpace(dto.id)) dto.id = Guid.NewGuid().ToString();
            await _repo.CreateAsync(dto);
        }

        public async Task UpdateAsync(FoodDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            //if (string.IsNullOrWhiteSpace(dto.id)) throw new ArgumentException("Id không hợp lệ.", nameof(dto));
            dto.name = NormalizeString(dto.name);
            dto.description = string.IsNullOrWhiteSpace(dto.description) ? null : NormalizeString(dto.description);
            dto.Images = dto.Images ?? new List<ImageDto>();
            dto.price = dto.price <= 0 ? 0 : dto.price;
            dto.sale = dto.sale < 0 ? 0 : dto.sale;
            dto.popular = dto.popular;
            await _repo.UpdateAsync(dto);
        }

        public async Task DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id không hợp lệ.", nameof(id));
            await _repo.DeleteAsync(id);
        }

        public async Task<(IEnumerable<FoodDto> Items, PaginationInfo Pagination)> QueryAsync(string? searchTerm, string? sortColumn, string? sortOrder, int page, int pageSize)
        {
            var all = (await _repo.GetAllAsync()) ?? Array.Empty<FoodDto>();
            IEnumerable<FoodDto> q = all;

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var t = searchTerm.Trim();
                q = q.Where(f => (!string.IsNullOrEmpty(f.name) && f.name.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            bool asc = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(sortColumn))
            {
                q = sortColumn switch
                {
                    "name" => asc ? q.OrderBy(f => f.name) : q.OrderByDescending(f => f.name),
                    "price" => asc ? q.OrderBy(f => f.price) : q.OrderByDescending(f => f.price),
                    "sale" => asc ? q.OrderBy(f => f.sale) : q.OrderByDescending(f => f.sale),
                    "popular" => asc ? q.OrderBy(f => f.popular) : q.OrderByDescending(f => f.popular),
                    _ => q
                };
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
