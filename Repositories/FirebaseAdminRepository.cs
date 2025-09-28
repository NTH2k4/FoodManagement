using FoodManagement.Contracts;
using FoodManagement.Models;
using FoodManagement.Repositories.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace FoodManagement.Repositories
{
    public class FirebaseAdminRepository : IAdminRepository, IRealtimeRepository<AdminDto>, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _databaseUrl;
        private readonly string? _authToken;
        private const string AdminNode = "adminAccounts";
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly FirebaseRealtimeSseListener _sseListener;
        private readonly ConcurrentDictionary<string, AdminDto> _store = new(StringComparer.Ordinal);
        private readonly object _storeLock = new();
        private CancellationTokenSource? _cts;
        private Task? _listenTask;
        private DateTime _lastPush = DateTime.UtcNow;
        private readonly ILogger<FirebaseAdminRepository> _logger;

        public event EventHandler<RealtimeUpdatedEventArgs<AdminDto>>? RealtimeUpdated;

        public FirebaseAdminRepository(IConfiguration configuration, ILogger<FirebaseAdminRepository> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = new HttpClient();
            _databaseUrl = configuration["Firebase:DatabaseUrl"]?.TrimEnd('/') ?? throw new InvalidOperationException("DatabaseUrl not configured");
            _authToken = configuration["Firebase:AuthToken"];

            _sseListener = new FirebaseRealtimeSseListener(_databaseUrl, AdminNode, _authToken, logger: logger as ILogger<FirebaseRealtimeSseListener>);
            _sseListener.OnMessage += HandleSseEvent;
        }

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

        private async Task<string?> FindAdminIdByPhoneAsync(string phone, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(phone)) return null;

            var norm = NormalizePhone(phone);
            if (string.IsNullOrEmpty(norm)) return null;

            foreach (var kv in _store)
            {
                var a = kv.Value;
                if (a == null) continue;
                if (NormalizePhone(a.phone) == norm) return a.id;
            }

            var baseUrl = $"{_databaseUrl}/{AdminNode}.json";
            var qOrderBy = "orderBy=" + Uri.EscapeDataString("\"phone\"");
            var qEqualTo = "equalTo=" + Uri.EscapeDataString("\"" + norm + "\"");
            var url = $"{baseUrl}?{qOrderBy}&{qEqualTo}";
            if (!string.IsNullOrEmpty(_authToken)) url += $"&auth={Uri.EscapeDataString(_authToken)}";

            try
            {
                var resp = await _httpClient.GetAsync(url, ct);
                if (!resp.IsSuccessStatusCode) return null;
                var json = await resp.Content.ReadAsStringAsync(ct);
                if (string.IsNullOrWhiteSpace(json) || json == "null") return null;

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return null;

                foreach (var prop in root.EnumerateObject())
                {
                    var maybeId = prop.Name;
                    return maybeId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "FindAdminIdByPhoneAsync fallback query failed");
            }

            return null;
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
            if (_store.Count > 0) return _store.Values.ToList();
            return await GetSnapshotAsync(ct);
        }

        public async Task<AdminDto?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(id)) return null;

            if (_store.TryGetValue(id, out var cached)) return cached;

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

            var normPhone = NormalizePhone(dto.phone);
            var existing = await FindAdminIdByPhoneAsync(normPhone, ct);
            if (existing != null)
            {
                throw new InvalidOperationException($"Số điện thoại đang được sử dụng bởi tài khoản khác.");
            }

            if (string.IsNullOrEmpty(dto.id)) dto.id = Guid.NewGuid().ToString();

            dto.phone = normPhone;
            dto.createdAt = DateTime.UtcNow;

            var resp = await _httpClient.PutAsJsonAsync(BuildUrl(dto.id), dto, ct);
            resp.EnsureSuccessStatusCode();
        }

        public async Task UpdateAsync(AdminDto dto, CancellationToken ct = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var normPhone = NormalizePhone(dto.phone);
            var existing = await FindAdminIdByPhoneAsync(normPhone, ct);
            if (existing != null && existing != dto.id)
            {
                throw new InvalidOperationException($"Số điện thoại đang được sử dụng bởi tài khoản khác.");
            }

            dto.phone = normPhone;

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

        public Task StartListeningAsync(CancellationToken ct = default)
        {
            if (_cts != null) return Task.CompletedTask;

            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _listenTask = Task.Run(async () =>
            {
                try
                {
                    await _sseListener.StartAsync(_cts.Token);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SSE listener failed (admins)");
                }
            }, _cts.Token);

            _ = Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), _cts.Token);
                        if (DateTime.UtcNow - _lastPush > TimeSpan.FromSeconds(30))
                        {
                            _logger.LogInformation("AdminRepo fallback snapshot fetch");
                            var all = await GetSnapshotAsync(_cts.Token);
                            RealtimeUpdated?.Invoke(this, new RealtimeUpdatedEventArgs<AdminDto>(all));
                            _lastPush = DateTime.UtcNow;
                        }
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "AdminRepo watchdog error");
                    }
                }
            }, _cts.Token);

            _logger.LogInformation("AdminRepo listening started");
            return Task.CompletedTask;
        }

        public Task StopListeningAsync(CancellationToken ct = default)
        {
            StopListening();
            return Task.CompletedTask;
        }

        public void StopListening()
        {
            try
            {
                _cts?.Cancel();
                _cts = null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while stopping listener");
            }
        }

        public async Task<IEnumerable<AdminDto>> GetSnapshotAsync(CancellationToken ct = default)
        {
            var url = BuildUrl();
            try
            {
                var resp = await _httpClient.GetAsync(url, ct);
                resp.EnsureSuccessStatusCode();
                var json = await resp.Content.ReadAsStringAsync(ct);
                if (string.IsNullOrWhiteSpace(json) || json == "null")
                {
                    lock (_storeLock)
                    {
                        _store.Clear();
                    }
                    return Array.Empty<AdminDto>();
                }

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

                lock (_storeLock)
                {
                    _store.Clear();
                    foreach (var kv in temp) _store[kv.Key] = kv.Value;
                }

                return _store.Values.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Snapshot fetch error (admins)");
                return _store.Values.ToList();
            }
        }

        private void MarkPush() => _lastPush = DateTime.UtcNow;

        private void HandleSseEvent(Infrastructure.FirebaseSseEvent evt)
        {
            try
            {
                MarkPush();

                var path = (evt.Path ?? "/").Trim();
                if (string.IsNullOrEmpty(path)) path = "/";

                if (path == "/")
                {
                    ApplyFullSnapshot(evt.RawData);
                    RealtimeUpdated?.Invoke(this, new RealtimeUpdatedEventArgs<AdminDto>(_store.Values.ToList()));
                    return;
                }

                var segs = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segs.Length >= 1)
                {
                    var adminId = segs[0];
                    if (evt.RawData == "null")
                    {
                        _store.TryRemove(adminId, out _);
                        RealtimeUpdated?.Invoke(this, new RealtimeUpdatedEventArgs<AdminDto>(_store.Values.ToList()));
                        return;
                    }

                    try
                    {
                        var dto = JsonSerializer.Deserialize<AdminDto>(evt.RawData, _jsonOptions);
                        if (dto != null)
                        {
                            dto.id ??= adminId;
                            _store[adminId] = dto;
                            RealtimeUpdated?.Invoke(this, new RealtimeUpdatedEventArgs<AdminDto>(_store.Values.ToList()));
                            return;
                        }

                        using var doc = JsonDocument.Parse($"{{ \"{adminId}\": {evt.RawData} }}");
                        ApplyMergePatch(doc.RootElement);
                        RealtimeUpdated?.Invoke(this, new RealtimeUpdatedEventArgs<AdminDto>(_store.Values.ToList()));
                    }
                    catch (JsonException je)
                    {
                        _logger.LogWarning(je, "Failed to parse SSE payload for admin {id}", adminId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HandleSseEvent error (admins)");
            }
        }

        private void ApplyFullSnapshot(string rawData)
        {
            if (string.IsNullOrWhiteSpace(rawData) || rawData == "null")
            {
                if (_store.Count > 0)
                {
                    return;
                }

                lock (_storeLock) { _store.Clear(); }
                return;
            }

            using var doc = JsonDocument.Parse(rawData);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return;

            var temp = new Dictionary<string, AdminDto>(StringComparer.Ordinal);
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
                    _logger.LogDebug(ex, "Failed to parse admin {id} in full snapshot", prop.Name);
                }
            }

            lock (_storeLock)
            {
                _store.Clear();
                foreach (var kv in temp) _store[kv.Key] = kv.Value;
            }
        }

        private void ApplyMergePatch(JsonElement rootElement)
        {
            if (rootElement.ValueKind != JsonValueKind.Object) return;

            foreach (var prop in rootElement.EnumerateObject())
            {
                var adminId = prop.Name;
                var value = prop.Value;
                if (value.ValueKind == JsonValueKind.Null)
                {
                    _store.TryRemove(adminId, out _);
                    continue;
                }

                try
                {
                    var dto = JsonSerializer.Deserialize<AdminDto>(value.GetRawText(), _jsonOptions);
                    if (dto != null)
                    {
                        dto.id ??= adminId;
                        _store[adminId] = dto;
                        continue;
                    }
                }
                catch { }
            }
        }

        public void Dispose() => StopListening();
    }
}
