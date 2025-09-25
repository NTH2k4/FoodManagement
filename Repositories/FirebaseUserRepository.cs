using FoodManagement.Contracts;
using FoodManagement.Models;
using FoodManagement.Repositories.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace FoodManagement.Repositories
{
    // Repository implements both classic IRepository<T> and your IRealtimeRepository<T>
    public class FirebaseUserRepository : IRepository<UserDto>, IRealtimeRepository<UserDto>, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _databaseUrl;
        private readonly string? _authToken;
        private const string UserNode = "users";
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly FirebaseRealtimeSseListener _sseListener;
        private readonly ConcurrentDictionary<string, UserDto> _store = new(StringComparer.Ordinal);
        private readonly object _storeLock = new();
        private CancellationTokenSource? _cts;
        private Task? _listenTask;
        private DateTime _lastPush = DateTime.UtcNow;
        private readonly ILogger<FirebaseUserRepository> _logger;

        // IRealtimeRepository event
        public event EventHandler<RealtimeUpdatedEventArgs<UserDto>>? RealtimeUpdated;

        public FirebaseUserRepository(IConfiguration configuration, ILogger<FirebaseUserRepository> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = new HttpClient();
            _databaseUrl = configuration["Firebase:DatabaseUrl"]?.TrimEnd('/') ?? throw new InvalidOperationException("DatabaseUrl not configured");
            _authToken = configuration["Firebase:AuthToken"];

            _sseListener = new FirebaseRealtimeSseListener(_databaseUrl, UserNode, _authToken, logger: logger as ILogger<FirebaseRealtimeSseListener>);
            _sseListener.OnMessage += HandleSseEvent;
        }

        private string BuildUrl(string? child = null)
        {
            var url = $"{_databaseUrl}/{UserNode}";
            if (!string.IsNullOrEmpty(child)) url += $"/{child}";
            url += ".json";
            if (!string.IsNullOrEmpty(_authToken)) url += $"?auth={_authToken}";
            return url;
        }

        // -------------------------
        // IRepository<T> methods
        // -------------------------
        public async Task<IEnumerable<UserDto>> GetAllAsync(CancellationToken ct = default)
        {
            // Prefer in-memory store if populated
            if (_store.Count > 0) return _store.Values.ToList();
            return await GetSnapshotAsync(ct);
        }

        public async Task<UserDto?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(id)) return null;

            if (_store.TryGetValue(id, out var cached)) return cached;

            var url = BuildUrl(id);
            var resp = await _httpClient.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode) return null;
            var json = await resp.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(json) || json == "null") return null;
            var dto = JsonSerializer.Deserialize<UserDto>(json, _jsonOptions);
            if (dto != null) dto.id ??= id;
            return dto;
        }

        public async Task CreateAsync(UserDto dto, CancellationToken ct = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrEmpty(dto.id)) dto.id = Guid.NewGuid().ToString();

            var resp = await _httpClient.PutAsJsonAsync(BuildUrl(dto.id), dto, ct);
            resp.EnsureSuccessStatusCode();
        }

        public async Task UpdateAsync(UserDto dto, CancellationToken ct = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrEmpty(dto.id)) throw new ArgumentException("id is required for update", nameof(dto));

            var resp = await _httpClient.PutAsJsonAsync(BuildUrl(dto.id), dto, ct);
            resp.EnsureSuccessStatusCode();
        }

        public async Task DeleteAsync(string id, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(id)) return;
            var resp = await _httpClient.DeleteAsync(BuildUrl(id), ct);
            resp.EnsureSuccessStatusCode();
        }

        // -------------------------
        // IRealtimeRepository<T> methods
        // -------------------------
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
                catch (OperationCanceledException) { /* graceful */ }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SSE listener failed (users)");
                }
            }, _cts.Token);

            // watchdog: fallback snapshot when no pushes
            _ = Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), _cts.Token);
                        if (DateTime.UtcNow - _lastPush > TimeSpan.FromSeconds(30))
                        {
                            _logger.LogInformation("UserRepo fallback snapshot fetch");
                            var all = await GetSnapshotAsync(_cts.Token);
                            RealtimeUpdated?.Invoke(this, new RealtimeUpdatedEventArgs<UserDto>(all));
                            _lastPush = DateTime.UtcNow;
                        }
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "UserRepo watchdog error");
                    }
                }
            }, _cts.Token);

            _logger.LogInformation("UserRepo listening started");
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

        public async Task<IEnumerable<UserDto>> GetSnapshotAsync(CancellationToken ct = default)
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
                    return Array.Empty<UserDto>();
                }

                var temp = new Dictionary<string, UserDto>(StringComparer.Ordinal);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return Array.Empty<UserDto>();

                foreach (var prop in root.EnumerateObject())
                {
                    try
                    {
                        var dto = JsonSerializer.Deserialize<UserDto>(prop.Value.GetRawText(), _jsonOptions);
                        if (dto == null) continue;
                        dto.id ??= prop.Name;
                        temp[prop.Name] = dto;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to parse user {id}", prop.Name);
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
                _logger.LogError(ex, "Snapshot fetch error (users)");
                // on error: return whatever we have in memory
                return _store.Values.ToList();
            }
        }

        // -------------------------
        // SSE handling
        // -------------------------
        private void MarkPush() => _lastPush = DateTime.UtcNow;

        private void HandleSseEvent(Infrastructure.FirebaseSseEvent evt)
        {
            try
            {
                MarkPush();

                var path = (evt.Path ?? "/").Trim();
                if (string.IsNullOrEmpty(path)) path = "/";

                // full root snapshot
                if (path == "/")
                {
                    ApplyFullSnapshot(evt.RawData);
                    RealtimeUpdated?.Invoke(this, new RealtimeUpdatedEventArgs<UserDto>(_store.Values.ToList()));
                    return;
                }

                // path like "/{userId}" or deeper (we treat first segment as user id)
                var segs = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segs.Length >= 1)
                {
                    var userId = segs[0];
                    if (evt.RawData == "null")
                    {
                        _store.TryRemove(userId, out _);
                        RealtimeUpdated?.Invoke(this, new RealtimeUpdatedEventArgs<UserDto>(_store.Values.ToList()));
                        return;
                    }

                    // evt.RawData is JSON representation of the user object (or a patch)
                    try
                    {
                        var dto = JsonSerializer.Deserialize<UserDto>(evt.RawData, _jsonOptions);
                        if (dto != null)
                        {
                            dto.id ??= userId;
                            _store[userId] = dto;
                            RealtimeUpdated?.Invoke(this, new RealtimeUpdatedEventArgs<UserDto>(_store.Values.ToList()));
                            return;
                        }

                        // if not a full object, attempt to parse as patch (object of children) -> reconstruct
                        using var doc = JsonDocument.Parse($"{{ \"{userId}\": {evt.RawData} }}");
                        ApplyMergePatch(doc.RootElement);
                        RealtimeUpdated?.Invoke(this, new RealtimeUpdatedEventArgs<UserDto>(_store.Values.ToList()));
                    }
                    catch (JsonException je)
                    {
                        _logger.LogWarning(je, "Failed to parse SSE payload for user {id}", userId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HandleSseEvent error (users)");
            }
        }

        private void ApplyFullSnapshot(string rawData)
        {
            if (string.IsNullOrWhiteSpace(rawData) || rawData == "null")
            {
                if (_store.Count > 0)
                {
                    _logger.LogWarning("Received root=null but local store not empty - ignoring transient null");
                    return;
                }

                lock (_storeLock) { _store.Clear(); }
                return;
            }

            using var doc = JsonDocument.Parse(rawData);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return;

            var temp = new Dictionary<string, UserDto>(StringComparer.Ordinal);
            foreach (var prop in root.EnumerateObject())
            {
                try
                {
                    var dto = JsonSerializer.Deserialize<UserDto>(prop.Value.GetRawText(), _jsonOptions);
                    if (dto == null) continue;
                    dto.id ??= prop.Name;
                    temp[prop.Name] = dto;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to parse user {id} in full snapshot", prop.Name);
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
                var userId = prop.Name;
                var value = prop.Value;
                if (value.ValueKind == JsonValueKind.Null)
                {
                    _store.TryRemove(userId, out _);
                    continue;
                }

                try
                {
                    var dto = JsonSerializer.Deserialize<UserDto>(value.GetRawText(), _jsonOptions);
                    if (dto != null)
                    {
                        dto.id ??= userId;
                        _store[userId] = dto;
                        continue;
                    }
                }
                catch
                {
                    // ignore parse errors for patch entries
                }
            }
        }

        // -------------------------
        // Cleanup
        // -------------------------
        public void Dispose() => StopListening();
    }
}
