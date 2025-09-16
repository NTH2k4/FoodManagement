using FoodManagement.Contracts;
using FoodManagement.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace FoodManagement.Repositories
{
    public class FirebaseFoodRepository : IRepository<FoodDto>
    {
        private readonly HttpClient _httpClient;
        private readonly string _databaseUrl;
        private const string FoodNode = "food";
        private readonly string? _authToken;

        public FirebaseFoodRepository(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _databaseUrl = configuration["Firebase:DatabaseUrl"] ?? throw new InvalidOperationException("DatabaseUrl not configured");
            _authToken = null;
        }

        private string BuildUrl(string? child = null)
        {
            var url = $"{_databaseUrl}/{FoodNode}";
            if (!string.IsNullOrEmpty(child))
                url += $"/{child}";
            url += ".json";
            if (!string.IsNullOrEmpty(_authToken))
                url += $"?auth={_authToken}";
            return url;
        }

        public async Task<IEnumerable<FoodDto>> GetAllAsync(CancellationToken ct = default)
        {
            var url = BuildUrl();
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(json) || json == "null") return new List<FoodDto>();
            var list = JsonSerializer.Deserialize<List<FoodDto>>(json) ?? new();
            return list.Where(x => x != null);
        }

        public async Task<FoodDto?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            var url = BuildUrl(id);
            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(json) || json == "null") return null;
            var dto = JsonSerializer.Deserialize<FoodDto>(json);
            if (dto != null && int.TryParse(id, out var intId))
                dto.id = intId;
            return dto;
        }

        public async Task CreateAsync(FoodDto dto, CancellationToken ct = default)
        {
            var allFoods = await GetAllAsync(ct);
            int maxId = allFoods.Any() ? allFoods.Max(f => f.id) : 0;
            var id = dto.id != 0 ? dto.id.ToString() : (maxId + 1).ToString();
            dto.id = int.Parse(id);

            var url = BuildUrl(id);
            var response = await _httpClient.PutAsJsonAsync(url, dto, ct);
            response.EnsureSuccessStatusCode();
        }

        public async Task UpdateAsync(FoodDto dto, CancellationToken ct = default)
        {
            if (dto.id == 0) throw new ArgumentException("Id is required for update");
            var url = BuildUrl(dto.id.ToString());
            var response = await _httpClient.PutAsJsonAsync(url, dto, ct);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteAsync(string id, CancellationToken ct = default)
        {
            var url = BuildUrl(id);
            var response = await _httpClient.DeleteAsync(url, ct);
            response.EnsureSuccessStatusCode();
        }
    }
}