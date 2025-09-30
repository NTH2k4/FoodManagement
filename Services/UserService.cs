using FoodManagement.Contracts;
using FoodManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoodManagement.Services
{
    public class UserService : IService<UserDto>
    {
        private readonly IRepository<UserDto> _repo;

        public UserService(IRepository<UserDto> repo) => _repo = repo;

        private static string NormalizeString(string? s) => string.IsNullOrWhiteSpace(s) ? string.Empty : s.Trim();

        private static string NormalizePhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return string.Empty;
            var s = phone.Trim();
            var sb = new System.Text.StringBuilder();
            foreach (var ch in s)
            {
                if (char.IsDigit(ch) || ch == '+') sb.Append(ch);
            }
            return sb.ToString();
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync() => await _repo.GetAllAsync();

        public async Task<UserDto?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return await _repo.GetByIdAsync(id);
        }

        public async Task CreateAsync(UserDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            dto.fullName = NormalizeString(dto.fullName);
            dto.email = string.IsNullOrWhiteSpace(dto.email) ? null : NormalizeString(dto.email);
            dto.phone = NormalizePhone(dto.phone);
            dto.address = string.IsNullOrWhiteSpace(dto.address) ? null : NormalizeString(dto.address);
            dto.createdAt = dto.createdAt == default ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : dto.createdAt;
            if (string.IsNullOrWhiteSpace(dto.id)) dto.id = Guid.NewGuid().ToString();

            var all = (await _repo.GetAllAsync()) ?? Array.Empty<UserDto>();
            var normPhone = dto.phone;
            if (!string.IsNullOrEmpty(normPhone) && all.Any(u => !string.IsNullOrEmpty(u.phone) && NormalizePhone(u.phone) == normPhone))
                throw new InvalidOperationException("Số điện thoại đang được sử dụng bởi tài khoản khác.");

            await _repo.CreateAsync(dto);
        }

        public async Task UpdateAsync(UserDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.id)) throw new ArgumentException("Id không hợp lệ.", nameof(dto));
            dto.fullName = NormalizeString(dto.fullName);
            dto.email = string.IsNullOrWhiteSpace(dto.email) ? null : NormalizeString(dto.email);
            dto.phone = NormalizePhone(dto.phone);
            dto.address = string.IsNullOrWhiteSpace(dto.address) ? null : NormalizeString(dto.address);

            var all = (await _repo.GetAllAsync()) ?? Array.Empty<UserDto>();
            var normPhone = dto.phone;
            if (!string.IsNullOrEmpty(normPhone) && all.Any(u => !string.IsNullOrEmpty(u.phone) && NormalizePhone(u.phone) == normPhone && u.id != dto.id))
                throw new InvalidOperationException("Số điện thoại đang được sử dụng bởi tài khoản khác.");

            await _repo.UpdateAsync(dto);
        }

        public async Task DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id không hợp lệ.", nameof(id));
            await _repo.DeleteAsync(id);
        }

        public async Task<(IEnumerable<UserDto> Items, PaginationInfo Pagination)> QueryAsync(string? searchTerm, string? sortColumn, string? sortOrder, int page, int pageSize)
        {
            var all = (await _repo.GetAllAsync()) ?? Array.Empty<UserDto>();
            IEnumerable<UserDto> q = all;

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var t = searchTerm.Trim();
                q = q.Where(u =>
                    (!string.IsNullOrEmpty(u.fullName) && u.fullName.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(u.email) && u.email.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(u.phone) && u.phone.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0)
                );
            }

            bool asc = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(sortColumn))
            {
                q = sortColumn switch
                {
                    "fullname" => asc ? q.OrderBy(u => u.fullName) : q.OrderByDescending(u => u.fullName),
                    "email" => asc ? q.OrderBy(u => u.email) : q.OrderByDescending(u => u.email),
                    "CreatedDate" => asc ? q.OrderBy(u => u.createdAt) : q.OrderByDescending(u => u.createdAt),
                    _ => q
                };
            }

            var totalItems = q.Count();
            if (pageSize <= 0) pageSize = 10;
            if (page <= 0) page = 1;
            var totalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalItems / pageSize) : 0;
            if (page > totalPages && totalPages > 0) page = totalPages;

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
