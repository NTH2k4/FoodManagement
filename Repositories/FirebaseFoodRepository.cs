using FoodManagement.Contracts;
using FoodManagement.Models;
using FoodManagement.Repositories.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace FoodManagement.Repositories
{
    public class FirebaseFoodRepository : IRepository<FoodDto>, IRealtimeRepository<FoodDto>, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _databaseUrl;
        private readonly string? _authToken;
        private const string FoodNode = "food";
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly FirebaseRealtimeSseListener _sseListener;
        private readonly ConcurrentDictionary<string, FoodDto> _store = new(StringComparer.Ordinal);
        private readonly object _storeLock = new();
        private CancellationTokenSource? _cts;
        private Task? _listenTask;
        private DateTime _lastPush = DateTime.UtcNow;
        private readonly ILogger<FirebaseFoodRepository> _logger;

        public event EventHandler<RealtimeUpdatedEventArgs<FoodDto>>? RealtimeUpdated;

        public FirebaseFoodRepository(IConfiguration configuration, ILogger<FirebaseFoodRepository> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _databaseUrl = configuration["Firebase:DatabaseUrl"]?.TrimEnd('/') ?? throw new InvalidOperationException("DatabaseUrl not configured");
            _authToken = configuration["Firebase:AuthToken"];

            _sseListener = new FirebaseRealtimeSseListener(_databaseUrl, FoodNode, _authToken, logger: logger as ILogger<FirebaseRealtimeSseListener>);
            _sseListener.OnMessage += HandleSseEventInternal;
        }

        private string BuildUrl(string? child = null)
        {
            var url = $"{_databaseUrl}/{FoodNode}";
            if (!string.IsNullOrEmpty(child)) url += $"/{child}";
            url += ".json";
            if (!string.IsNullOrEmpty(_authToken)) url += $"?auth={_authToken}";
            return url;
        }

        // -------------------------
        // IRealtimeRepository impl
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
                catch (OperationCanceledException) { }
                catch (Exception ex) { _logger.LogError(ex, "[FoodRepo] SSE listener failed"); }
            }, _cts.Token);

            // watchdog: snapshot fallback if no event for a while
            _ = Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(20), _cts.Token);
                        if (DateTime.UtcNow - _lastPush > TimeSpan.FromSeconds(40))
                        {
                            _logger.LogInformation("[FoodRepo] No SSE events recently; fetching snapshot as fallback.");
                            var all = await FetchAndPopulateStore(_cts.Token);
                            RealtimeUpdated?.Invoke(this, new RealtimeUpdatedEventArgs<FoodDto>(all));
                            _lastPush = DateTime.UtcNow;
                        }
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex) { _logger.LogError(ex, "[FoodRepo] Watchdog error"); }
                }
            }, _cts.Token);

            _logger.LogInformation("[FoodRepo] StartListeningAsync started.");
            return Task.CompletedTask;
        }

        public Task StopListeningAsync(CancellationToken ct = default)
        {
            try
            {
                _cts?.Cancel();
                _cts = null;
            }
            catch { }
            return Task.CompletedTask;
        }

        // Return current snapshot (store if present, else fetch)
        public async Task<IEnumerable<FoodDto>> GetSnapshotAsync(CancellationToken ct = default)
        {
            if (_store.Count > 0) return _store.Values.ToList();
            return await FetchAndPopulateStore(ct);
        }

        // -------------------------
        // Internal SSE handling
        // -------------------------
        private void MarkPush() => _lastPush = DateTime.UtcNow;

        private void HandleSseEventInternal(Infrastructure.FirebaseSseEvent evt)
        {
            try
            {
                MarkPush();

                var path = (evt.Path ?? "/").Trim();
                if (string.IsNullOrEmpty(path)) path = "/";

                if (path == "/")
                {
                    UpdateFullTree(evt.RawData);
                    RealtimeUpdated?.Invoke(this, new RealtimeUpdatedEventArgs<FoodDto>(_store.Values.ToList()));
                    return;
                }

                // path like "/<id>"
                var segs = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segs.Length >= 1)
                {
                    var itemId = segs[0];
                    if (evt.RawData == "null")
                    {
                        _store.TryRemove(itemId, out _);
                        RealtimeUpdated?.Invoke(this, new RealtimeUpdatedEventArgs<FoodDto>(_store.Values.ToList()));
                        return;
                    }

                    // payload can be object for single element
                    try
                    {
                        var dto = JsonSerializer.Deserialize<FoodDto>(evt.RawData, _jsonOptions);
                        if (dto != null)
                        {
                            // Ensure id consistency
                            if (dto.id == 0 && int.TryParse(itemId, out var parsed)) dto.id = parsed;
                            _store[itemId] = dto;
                            RealtimeUpdated?.Invoke(this, new RealtimeUpdatedEventArgs<FoodDto>(_store.Values.ToList()));
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "[FoodRepo] Failed parse single item from SSE id={id}", itemId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FoodRepo] HandleSseEvent error");
            }
        }

        private void UpdateFullTree(string rawData)
        {
            // Accept array or object; build temp map then swap
            if (string.IsNullOrWhiteSpace(rawData) || rawData == "null")
            {
                // If we already have data, ignore possible transient null
                if (_store.Count > 0)
                {
                    _logger.LogWarning("[FoodRepo] Received root=null but local store has {count}; ignoring transient null.", _store.Count);
                    return;
                }
                lock (_storeLock) { _store.Clear(); }
                return;
            }

            try
            {
                using var doc = JsonDocument.Parse(rawData);
                var root = doc.RootElement;
                var temp = new Dictionary<string, FoodDto>(StringComparer.Ordinal);

                if (root.ValueKind == JsonValueKind.Object)
                {
                    // object map: keys might be numeric ids or random keys
                    foreach (var prop in root.EnumerateObject())
                    {
                        try
                        {
                            var dto = JsonSerializer.Deserialize<FoodDto>(prop.Value.GetRawText(), _jsonOptions);
                            if (dto != null)
                            {
                                if (dto.id == 0 && int.TryParse(prop.Name, out var parsed)) dto.id = parsed;
                                temp[prop.Name] = dto;
                            }
                        }
                        catch { /* skip malformed */ }
                    }
                }
                else if (root.ValueKind == JsonValueKind.Array)
                {
                    // array: index-based map or elements may have id inside
                    var idx = 0;
                    foreach (var item in root.EnumerateArray())
                    {
                        try
                        {
                            var dto = JsonSerializer.Deserialize<FoodDto>(item.GetRawText(), _jsonOptions);
                            if (dto != null)
                            {
                                var key = dto.id != 0 ? dto.id.ToString() : idx.ToString();
                                temp[key] = dto;
                            }
                        }
                        catch { }
                        idx++;
                    }
                }

                lock (_storeLock)
                {
                    _store.Clear();
                    foreach (var kv in temp) _store[kv.Key] = kv.Value;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "[FoodRepo] UpdateFullTree parse error");
            }
        }

        private async Task<IEnumerable<FoodDto>> FetchAndPopulateStore(CancellationToken ct)
        {
            var url = BuildUrl();
            try
            {
                var response = await _httpClient.GetAsync(url, ct);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync(ct);
                if (string.IsNullOrWhiteSpace(json) || json == "null")
                {
                    if (_store.Count > 0) return _store.Values.ToList();
                    lock (_storeLock) { _store.Clear(); }
                    return Array.Empty<FoodDto>();
                }

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var temp = new Dictionary<string, FoodDto>(StringComparer.Ordinal);

                if (root.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in root.EnumerateObject())
                    {
                        try
                        {
                            var dto = JsonSerializer.Deserialize<FoodDto>(prop.Value.GetRawText(), _jsonOptions);
                            if (dto != null)
                            {
                                if (dto.id == 0 && int.TryParse(prop.Name, out var parsed)) dto.id = parsed;
                                temp[prop.Name] = dto;
                            }
                        }
                        catch { }
                    }
                }
                else if (root.ValueKind == JsonValueKind.Array)
                {
                    var idx = 0;
                    foreach (var item in root.EnumerateArray())
                    {
                        try
                        {
                            var dto = JsonSerializer.Deserialize<FoodDto>(item.GetRawText(), _jsonOptions);
                            if (dto != null)
                            {
                                var key = dto.id != 0 ? dto.id.ToString() : idx.ToString();
                                temp[key] = dto;
                            }
                        }
                        catch { }
                        idx++;
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
                _logger.LogError(ex, "[FoodRepo] Snapshot fetch error");
                return _store.Values.ToList();
            }
        }

        // -------------------------
        // IRepository impl (wraps HTTP operations)
        // -------------------------
        public async Task<IEnumerable<FoodDto>> GetAllAsync(CancellationToken ct = default)
        {
            // prefer in-memory store
            if (_store.Count > 0) return _store.Values.ToList();
            return await FetchAndPopulateStore(ct);
        }

        public async Task<FoodDto?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_store.TryGetValue(id, out var found)) return found;
            var all = await GetAllAsync(ct);
            return all.FirstOrDefault(f => f.id.ToString() == id);
        }

        public async Task CreateAsync(FoodDto dto, CancellationToken ct = default)
        {
            // maintain existing behavior: determine id
            var all = await GetAllAsync(ct);
            int maxId = all.Any() ? all.Max(f => f.id) : 0;
            var id = dto.id != 0 ? dto.id.ToString() : (maxId + 1).ToString();
            dto.id = int.Parse(id);
            var resp = await _httpClient.PutAsJsonAsync(BuildUrl(id), dto, ct);
            resp.EnsureSuccessStatusCode();
        }

        public async Task UpdateAsync(FoodDto dto, CancellationToken ct = default)
        {
            if (dto.id == 0) throw new ArgumentException("Id is required for update");
            var resp = await _httpClient.PutAsJsonAsync(BuildUrl(dto.id.ToString()), dto, ct);
            resp.EnsureSuccessStatusCode();
        }

        public async Task DeleteAsync(string id, CancellationToken ct = default)
        {
            var resp = await _httpClient.DeleteAsync(BuildUrl(id), ct);
            resp.EnsureSuccessStatusCode();
        }

        public void Dispose()
        {
            try { _cts?.Cancel(); } catch { }
        }
    }
}
