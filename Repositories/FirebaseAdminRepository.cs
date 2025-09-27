using FoodManagement.Contracts;
using FoodManagement.Models;
using System.Text.Json;

namespace FoodManagement.Repositories
{
    public class FirebaseAdminRepository : IAdminRepository
    {
        private readonly HttpClient _httpClient;
        private readonly string _databaseUrl;
        private readonly string? _authToken;
        private const string AdminNode = "adminAccounts";
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
        private readonly ILogger<FirebaseAdminRepository> _logger;

        public FirebaseAdminRepository(IConfiguration configuration, ILogger<FirebaseAdminRepository> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _databaseUrl = configuration["Firebase:DatabaseUrl"]?.TrimEnd('/') ?? throw new InvalidOperationException("DatabaseUrl not configured");
            _authToken = configuration["Firebase:AuthToken"];
        }

        private string BuildUrl(string? child = null)
        {
            var url = $"{_databaseUrl}/{AdminNode}";
            if (!string.IsNullOrEmpty(child)) url += $"/{child}";
            url += ".json";
            if (!string.IsNullOrEmpty(_authToken)) url += $"?auth={_authToken}";
            return url;
        }

        public async Task<IEnumerable<AdminDto>> GetAllAsync(CancellationToken ct = default)
        {
            var url = BuildUrl();
            var resp = await _httpClient.GetAsync(url, ct);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(json) || json == "null") return Array.Empty<AdminDto>();
            var temp = new Dictionary<string, AdminDto>(StringComparer.Ordinal);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return Array.Empty<AdminDto>();
            foreach (var prop in root.EnumerateObject())
            {
                try
                {
                    var dto = JsonSerializer.Deserialize<AdminDto>(prop.Value.GetRawText(), _jsonOptions);
                    if (dto == null) continue;
                    dto.id ??= prop.Name;
                    temp[prop.Name] = dto;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to parse admin {id}", prop.Name);
                }
            }
            return temp.Values.ToList();
        }

        public async Task<AdminDto?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var url = BuildUrl(id);
            var resp = await _httpClient.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode) return null;
            var json = await resp.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(json) || json == "null") return null;
            var dto = JsonSerializer.Deserialize<AdminDto>(json, _jsonOptions);
            if (dto != null) dto.id ??= id;
            return dto;
        }

        public async Task CreateAsync(AdminDto dto, CancellationToken ct = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrEmpty(dto.id)) dto.id = Guid.NewGuid().ToString();
            dto.createdAt = DateTime.UtcNow;
            var resp = await _httpClient.PutAsJsonAsync(BuildUrl(dto.id), dto, ct);
            resp.EnsureSuccessStatusCode();
        }

        public async Task UpdateAsync(AdminDto dto, CancellationToken ct = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrEmpty(dto.id)) throw new ArgumentException("id is required for update");
            var resp = await _httpClient.PutAsJsonAsync(BuildUrl(dto.id), dto, ct);
            resp.EnsureSuccessStatusCode();
        }

        public async Task DeleteAsync(string id, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(id)) return;
            var resp = await _httpClient.DeleteAsync(BuildUrl(id), ct);
            resp.EnsureSuccessStatusCode();
        }

        public async Task<AdminDto?> GetByUsernameAsync(string username, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(username)) return null;
            var all = await GetAllAsync(ct);
            return all.FirstOrDefault(a => a.username.Equals(username, StringComparison.OrdinalIgnoreCase));
        }
    }
}
