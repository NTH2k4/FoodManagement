using FoodManagement.Contracts;
using FoodManagement.Models;
using System.Text.Json;

namespace FoodManagement.Repositories
{
    public class FirebaseFeedbackRepository : IRepository<FeedbackDto>
    {
        private readonly HttpClient _httpClient;
        private readonly string _databaseUrl;
        private const string BookingNode = "feedback";
        private readonly string? _authToken;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public FirebaseFeedbackRepository(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _databaseUrl = configuration["Firebase:DatabaseUrl"] ?? throw new InvalidOperationException("DatabaseUrl not configured");
            _authToken = null;
        }

        private string BuildUrl(string? child = null)
        {
            var url = $"{_databaseUrl}/{BookingNode}";
            if (!string.IsNullOrEmpty(child))
                url += $"/{child}";
            url += ".json";
            if (!string.IsNullOrEmpty(_authToken))
                url += $"?auth={_authToken}";
            return url;
        }

        public async Task DeleteAsync(string id, CancellationToken ct = default)
        {
            var all = GetAllAsync(ct).Result;
            var found = all.FirstOrDefault(b => b.id == id);
            if (found == null)
            {
                return;
            }
            var child = $"{found.accountId}/{found.id}";
            var response = await _httpClient.DeleteAsync(BuildUrl(child), ct);
            response.EnsureSuccessStatusCode();
        }

        public async Task<IEnumerable<FeedbackDto>> GetAllAsync(CancellationToken ct = default)
        {
            var url = BuildUrl();
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(json) || json == "null") return new List<FeedbackDto>();
            var dict = JsonSerializer.Deserialize<Dictionary<string, FeedbackDto>>(json, _jsonOptions) ?? new();
            var list = new List<FeedbackDto>(dict.Count);
            foreach (var kvp in dict)
            {
                var dto = kvp.Value;
                dto.id = kvp.Key;
                list.Add(dto);
            }
            return list;
        }

        public async Task<FeedbackDto?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var url = BuildUrl(id);
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(json) || json == "null") return null;
            var dto = JsonSerializer.Deserialize<FeedbackDto>(json, _jsonOptions);
            if (dto != null) dto.id = id;
            return dto;
        }

        public Task UpdateAsync(FeedbackDto dto, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task CreateAsync(FeedbackDto dto, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }
}
