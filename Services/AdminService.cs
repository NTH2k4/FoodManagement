using FoodManagement.Contracts;
using FoodManagement.Models;
using FoodManagement.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace FoodManagement.Services
{
    public class AdminService : IAdminService
    {
        private readonly IRepository<AdminDto> _repo;
        private readonly IPasswordHasher _hasher;

        public AdminService(IRepository<AdminDto> repo, IPasswordHasher hasher)
        {
            _repo = repo;
            _hasher = hasher;
        }

        private static string NormalizeString(string? s) => string.IsNullOrWhiteSpace(s) ? string.Empty : s.Trim();

        public async Task<IEnumerable<AdminDto>> GetAllAsync() => await _repo.GetAllAsync();

        public async Task<AdminDto?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return await _repo.GetByIdAsync(id);
        }

        public async Task CreateAsync(AdminDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            dto.username = NormalizeString(dto.username);
            dto.phone = NormalizeString(dto.phone);
            dto.email = string.IsNullOrWhiteSpace(dto.email) ? null : NormalizeString(dto.email);
            dto.firstName = NormalizeString(dto.firstName);
            dto.lastName = NormalizeString(dto.lastName);
            dto.createdAt = dto.createdAt == default ? DateTime.UtcNow : dto.createdAt;
            if (string.IsNullOrWhiteSpace(dto.id)) dto.id = Guid.NewGuid().ToString();
            var all = (await _repo.GetAllAsync()) ?? Array.Empty<AdminDto>();
            if (all.Any(a => !string.IsNullOrEmpty(a.username) && a.username.Equals(dto.username, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Tên đăng nhập đã tồn tại.");
            var normPhone = dto.phone;
            if (!string.IsNullOrEmpty(normPhone) && all.Any(a => !string.IsNullOrEmpty(a.phone) && a.phone == normPhone))
                throw new InvalidOperationException("Số điện thoại đang được sử dụng bởi tài khoản khác.");

            if (string.IsNullOrWhiteSpace(dto.passwordHashBase64))
            {
                var pwdProp = dto.GetType().GetProperty("password", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (pwdProp != null)
                {
                    var plain = pwdProp.GetValue(dto) as string;
                    if (!string.IsNullOrWhiteSpace(plain))
                    {
                        var (hashBase64, saltBase64) = _hasher.HashPassword(plain);
                        dto.passwordHashBase64 = hashBase64;
                        dto.passwordSaltBase64 = saltBase64;
                        if (pwdProp.CanWrite) pwdProp.SetValue(dto, null);
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(dto.passwordHashBase64))
                throw new InvalidOperationException("Mật khẩu không hợp lệ.");

            await _repo.CreateAsync(dto);
        }

        public async Task UpdateAsync(AdminDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.id)) throw new ArgumentException("Id không hợp lệ.", nameof(dto));
            dto.username = NormalizeString(dto.username);
            dto.phone = NormalizeString(dto.phone);
            dto.email = string.IsNullOrWhiteSpace(dto.email) ? null : NormalizeString(dto.email);
            dto.firstName = NormalizeString(dto.firstName);
            dto.lastName = NormalizeString(dto.lastName);

            var existing = await _repo.GetByIdAsync(dto.id);
            if (existing == null) throw new InvalidOperationException("Không tìm thấy tài khoản để cập nhật.");

            var all = (await _repo.GetAllAsync()) ?? Array.Empty<AdminDto>();
            if (all.Any(a => !string.IsNullOrEmpty(a.username) && a.username.Equals(dto.username, StringComparison.OrdinalIgnoreCase) && a.id != dto.id))
                throw new InvalidOperationException("Tên đăng nhập đang được sử dụng bởi tài khoản khác.");
            var normPhone = dto.phone;
            if (!string.IsNullOrEmpty(normPhone) && all.Any(a => !string.IsNullOrEmpty(a.phone) && a.phone == normPhone && a.id != dto.id))
                throw new InvalidOperationException("Số điện thoại đang được sử dụng bởi tài khoản khác.");

            var pwdProp = dto.GetType().GetProperty("password", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (pwdProp != null)
            {
                var plain = pwdProp.GetValue(dto) as string;
                if (!string.IsNullOrWhiteSpace(plain))
                {
                    var (hashBase64, saltBase64) = _hasher.HashPassword(plain);
                    dto.passwordHashBase64 = hashBase64;
                    dto.passwordSaltBase64 = saltBase64;
                    if (pwdProp.CanWrite) pwdProp.SetValue(dto, null);
                }
                else
                {
                    dto.passwordHashBase64 = existing.passwordHashBase64;
                    dto.passwordSaltBase64 = existing.passwordSaltBase64;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.passwordHashBase64))
                {
                    dto.passwordHashBase64 = existing.passwordHashBase64;
                    dto.passwordSaltBase64 = existing.passwordSaltBase64;
                }
            }

            await _repo.UpdateAsync(dto);
        }

        public async Task DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id không hợp lệ.", nameof(id));
            await _repo.DeleteAsync(id);
        }

        public async Task<(IEnumerable<AdminDto> Items, PaginationInfo Pagination)> QueryAsync(string? searchTerm, string? sortColumn, string? sortOrder, int page, int pageSize)
        {
            var all = (await _repo.GetAllAsync()) ?? Array.Empty<AdminDto>();
            IEnumerable<AdminDto> q = all;
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var t = searchTerm.Trim();
                q = q.Where(a =>
                    (!string.IsNullOrEmpty(a.username) && a.username.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(a.firstName) && a.firstName.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(a.lastName) && a.lastName.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(a.email) && a.email.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(a.phone) && a.phone.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0)
                );
            }
            bool asc = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(sortColumn))
            {
                q = sortColumn switch
                {
                    "fullname" => asc ? q.OrderBy(a => a.lastName).ThenBy(a => a.firstName) : q.OrderByDescending(a => a.lastName).ThenByDescending(a => a.firstName),
                    "firstName" => asc ? q.OrderBy(a => a.firstName) : q.OrderByDescending(a => a.firstName),
                    "lastName" => asc ? q.OrderBy(a => a.lastName) : q.OrderByDescending(a => a.lastName),
                    "username" => asc ? q.OrderBy(a => a.username) : q.OrderByDescending(a => a.username),
                    "email" => asc ? q.OrderBy(a => a.email) : q.OrderByDescending(a => a.email),
                    "phone" => asc ? q.OrderBy(a => a.phone) : q.OrderByDescending(a => a.phone),
                    "role" => asc ? q.OrderBy(a => a.role) : q.OrderByDescending(a => a.role),
                    "LastLogin" => asc ? q.OrderBy(a => a.LastLoginAt) : q.OrderByDescending(a => a.LastLoginAt),
                    "isActive" => asc ? q.OrderBy(a => a.isActive) : q.OrderByDescending(a => a.isActive),
                    "CreatedDate" => asc ? q.OrderBy(a => a.createdAt) : q.OrderByDescending(a => a.createdAt),
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

        public async Task ChangePasswordAsync(string adminId, string currentPassword, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(adminId)) throw new ArgumentException("Id không hợp lệ.", nameof(adminId));
            var admin = await _repo.GetByIdAsync(adminId);
            if (admin == null) throw new InvalidOperationException("Không tìm thấy tài khoản.");
            if (!_hasher.VerifyPassword(currentPassword, admin.passwordHashBase64, admin.passwordSaltBase64))
                throw new InvalidOperationException("Mật khẩu hiện tại không đúng.");
            var (hashBase64, saltBase64) = _hasher.HashPassword(newPassword);
            admin.passwordSaltBase64 = saltBase64;
            admin.passwordHashBase64 = hashBase64;
            await _repo.UpdateAsync(admin);
        }
    }
}
