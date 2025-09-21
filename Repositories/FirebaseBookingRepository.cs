using FoodManagement.Contracts;
using FoodManagement.Models;
using System.Text.Json;

namespace FoodManagement.Repositories
{
    public class FirebaseBookingRepository : IRepository<BookingDto>
    {
        private readonly HttpClient _httpClient;
        private readonly string _databaseUrl;
        private const string BookingNode = "booking";
        private readonly string? _authToken;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public FirebaseBookingRepository(IConfiguration configuration)
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

        public async Task CreateAsync(BookingDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(dto.accountId))
                throw new ArgumentException("accountId is required when creating booking.");

            if (dto.id == 0)
                dto.id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var child = $"{dto.accountId}/{dto.id}";
            var response = await _httpClient.PutAsJsonAsync(BuildUrl(child), dto, ct);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteAsync(string id, CancellationToken ct = default)
        {
            var all = await GetAllAsync(ct);
            var found = all.FirstOrDefault(b => b.id.ToString() == id);
            if (found == null)
            {
                return;
            }
            var child = $"{found.accountId}/{found.id}";
            var response = await _httpClient.DeleteAsync(BuildUrl(child), ct);
            response.EnsureSuccessStatusCode();
        }

        public async Task<IEnumerable<BookingDto>> GetAllAsync(CancellationToken ct = default)
        {
            var url = BuildUrl();
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(json) || json == "null") return new List<BookingDto>();

            // JSON shape: { "<accountId>": { "<bookingId>": {BookingDto}, ... }, ... }
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var result = new List<BookingDto>();

                if (root.ValueKind != JsonValueKind.Object) return result;

                foreach (var accProp in root.EnumerateObject())
                {
                    var accountId = accProp.Name;
                    var inner = accProp.Value;
                    if (inner.ValueKind != JsonValueKind.Object) continue;

                    foreach (var bookingProp in inner.EnumerateObject())
                    {
                        var bookingJson = bookingProp.Value.GetRawText();
                        var booking = JsonSerializer.Deserialize<BookingDto>(bookingJson, _jsonOptions);
                        if (booking != null)
                        {
                            if (booking.id == 0)
                            {
                                if (long.TryParse(bookingProp.Name, out var parsedId))
                                    booking.id = parsedId;
                            }
                            if (string.IsNullOrEmpty(booking.accountId))
                                booking.accountId = accountId;

                            result.Add(booking);
                        }
                    }
                }

                return result;
            }
            catch (JsonException)
            {
                return new List<BookingDto>();
            }
        }

        public async Task<BookingDto?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            var all = await GetAllAsync(ct);
            var found = all.FirstOrDefault(b => b.id.ToString() == id);
            return found;
        }

        public async Task UpdateAsync(BookingDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(dto.accountId))
            {
                var existing = await GetByIdAsync(dto.id.ToString(), ct);
                if (existing == null || string.IsNullOrEmpty(existing.accountId))
                    throw new ArgumentException("accountId is required to update booking.");
                dto.accountId = existing.accountId;
            }

            var child = $"{dto.accountId}/{dto.id}";
            var response = await _httpClient.PutAsJsonAsync(BuildUrl(child), dto, ct);
            response.EnsureSuccessStatusCode();
        }
    }
}
