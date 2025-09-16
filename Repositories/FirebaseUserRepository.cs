using FoodManagement.Contracts;
using FoodManagement.Models;

namespace FoodManagement.Repositories
{
    public class FirebaseUserRepository : IRepository<UserDto>
    {
        private readonly HttpClient _httpClient;
        private readonly string _databaseUrl;
        private const string UserNode = "users";
        private readonly string? _authToken;

        public FirebaseUserRepository(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _databaseUrl = configuration["Firebase:DatabaseUrl"] ?? throw new InvalidOperationException("DatabaseUrl not configured");
            _authToken = null;
        }

        private string BuildUrl(string? child = null)
        {
            var url = $"{_databaseUrl}/{UserNode}";
            if (!string.IsNullOrEmpty(child))
                url += $"/{child}";
            url += ".json";
            if (!string.IsNullOrEmpty(_authToken))
                url += $"?auth={_authToken}";
            return url;
        }

        public async Task CreateAsync(UserDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(dto.id))
                dto.id = Guid.NewGuid().ToString();
            var response = await _httpClient.PutAsJsonAsync(BuildUrl(dto.id), dto, ct);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteAsync(string id, CancellationToken ct = default)
        {
            var response = await _httpClient.DeleteAsync(BuildUrl(id), ct);
            response.EnsureSuccessStatusCode();
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync(CancellationToken ct = default)
        {
            var url = BuildUrl();
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(json) || json == "null") return new List<UserDto>();
            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, UserDto>>(json) ?? new();
            return dict.Values.Where(x => x != null);
        }

        public async Task<UserDto?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            var url = BuildUrl(id);
            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(json) || json == "null") return null;
            return System.Text.Json.JsonSerializer.Deserialize<UserDto>(json);
        }

        public async Task UpdateAsync(UserDto dto, CancellationToken ct = default)
        {
            var response = await _httpClient.PutAsJsonAsync(BuildUrl(dto.id), dto, ct);
            response.EnsureSuccessStatusCode();
        }
    }
}
