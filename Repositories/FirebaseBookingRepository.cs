using FoodManagement.Contracts;
using FoodManagement.Models;
using FoodManagement.Repositories.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace FoodManagement.Repositories
{
    public class FirebaseBookingRepository : IRepository<BookingDto>, IRealtimeRepository<BookingDto>, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _databaseUrl;
        private readonly string? _authToken;
        private const string BookingNode = "booking";
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly FirebaseRealtimeSseListener _sseListener;
        private readonly ConcurrentDictionary<string, BookingDto> _store = new(StringComparer.Ordinal);
        private readonly object _storeLock = new();
        private CancellationTokenSource? _cts;
        private Task? _listenTask;
        private DateTime _lastPush = DateTime.UtcNow;
        private readonly ILogger<FirebaseBookingRepository> _logger;

        // IRealtimeRepository<T> event
        public event EventHandler<RealtimeUpdatedEventArgs<BookingDto>>? RealtimeUpdated;

        public FirebaseBookingRepository(IConfiguration configuration, ILogger<FirebaseBookingRepository> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _databaseUrl = configuration["Firebase:DatabaseUrl"]?.TrimEnd('/') ?? throw new InvalidOperationException("DatabaseUrl not configured");
            _authToken = configuration["Firebase:AuthToken"];

            _sseListener = new FirebaseRealtimeSseListener(_databaseUrl, BookingNode, _authToken, logger: logger as ILogger<FirebaseRealtimeSseListener>);
            _sseListener.OnMessage += HandleSseEvent;
        }

        public bool IsListening => _cts != null;

        public Task StartListeningAsync(CancellationToken ct = default)
        {
            if (_cts != null) return Task.CompletedTask;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            _listenTask = Task.Run(async () =>
            {
                try
                {
                    await _sseListener.StartAsync(_cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { /* expected on shutdown */ }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Repo] SSE listener failed");
                }
            }, _cts.Token);

            // Watchdog: fallback snapshot fetch when no push seen recently
            _ = Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), _cts.Token).ConfigureAwait(false);
                        if (DateTime.UtcNow - _lastPush > TimeSpan.FromSeconds(30))
                        {
                            _logger.LogInformation("[Repo] No SSE push seen recently, performing snapshot fetch (fallback).");
                            var all = await FetchAndPopulateStore(_cts.Token).ConfigureAwait(false);
                            RaiseRealtimeUpdated(all);
                            _lastPush = DateTime.UtcNow;
                        }
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[Repo] Watchdog error");
                    }
                }
            }, _cts.Token);

            _logger.LogInformation("[Repo] Listening started.");
            return Task.CompletedTask;
        }

        public Task StopListeningAsync(CancellationToken ct = default)
        {
            try
            {
                _cts?.Cancel();
                _cts = null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Repo] StopListeningAsync error");
            }
            return Task.CompletedTask;
        }

        private void MarkPush() => _lastPush = DateTime.UtcNow;

        private void HandleSseEvent(FirebaseSseEvent evt)
        {
            try
            {
                MarkPush();
                _logger.LogDebug("[Repo] SSE event path={path}", evt.Path);

                var path = (evt.Path ?? "/").Trim();
                if (string.IsNullOrEmpty(path)) path = "/";

                if (path == "/")
                {
                    UpdateFullTree(evt.RawData);
                    RaiseRealtimeUpdated(_store.Values.ToList());
                    return;
                }

                var segs = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segs.Length == 1)
                {
                    var accountId = segs[0];
                    if (evt.RawData == "null")
                    {
                        var keys = _store.Where(kv => kv.Value.accountId == accountId).Select(kv => kv.Key).ToList();
                        foreach (var k in keys) _store.TryRemove(k, out _);
                    }
                    else
                    {
                        using var doc = JsonDocument.Parse($"{{ \"{accountId}\": {evt.RawData} }}");
                        MergeAccountNested(doc.RootElement);
                    }

                    RaiseRealtimeUpdated(_store.Values.ToList());
                    return;
                }

                if (segs.Length >= 2)
                {
                    var accountId = segs[0];
                    var bookingId = segs[1];
                    if (evt.RawData == "null")
                    {
                        _store.TryRemove(bookingId, out _);
                    }
                    else
                    {
                        var dto = JsonSerializer.Deserialize<BookingDto>(evt.RawData, _jsonOptions);
                        if (dto != null)
                        {
                            if (dto.id == 0 && long.TryParse(bookingId, out var parsed)) dto.id = parsed;
                            dto.accountId = dto.accountId ?? accountId;
                            _store[bookingId] = dto;
                        }
                    }

                    RaiseRealtimeUpdated(_store.Values.ToList());
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Repo] Error processing SSE event");
            }
        }

        private void UpdateFullTree(string rawData)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rawData) || rawData == "null")
                {
                    if (_store.Count > 0)
                    {
                        //_logger.LogWarning("[Repo] Received root=null from Firebase SSE but local store has {count} items. Ignoring transient null.", _store.Count);
                        return;
                    }

                    lock (_storeLock) { _store.Clear(); }
                    _logger.LogInformation("[Repo] Root is null and local store was empty. Store remains empty.");
                    return;
                }

                using var doc = JsonDocument.Parse(rawData);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return;

                var temp = new Dictionary<string, BookingDto>(StringComparer.Ordinal);

                foreach (var accProp in root.EnumerateObject())
                {
                    var accountId = accProp.Name;
                    var inner = accProp.Value;
                    if (inner.ValueKind != JsonValueKind.Object) continue;

                    foreach (var bookingProp in inner.EnumerateObject())
                    {
                        try
                        {
                            var dto = JsonSerializer.Deserialize<BookingDto>(bookingProp.Value.GetRawText(), _jsonOptions);
                            if (dto == null) continue;
                            if (dto.id == 0 && long.TryParse(bookingProp.Name, out var parsed)) dto.id = parsed;
                            dto.accountId = dto.accountId ?? accountId;
                            temp[bookingProp.Name] = dto;
                        }
                        catch (JsonException jex)
                        {
                            _logger.LogDebug(jex, "[Repo] UpdateFullTree: parse error for booking {id}", bookingProp.Name);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "[Repo] UpdateFullTree: unexpected error for booking {id}", bookingProp.Name);
                        }
                    }
                }

                lock (_storeLock)
                {
                    _store.Clear();
                    foreach (var kv in temp) _store[kv.Key] = kv.Value;
                }

                _logger.LogInformation("[Repo] Full tree updated from Firebase, entries={count}", _store.Count);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "[Repo] Full tree parse error");
            }
        }

        private void MergeAccountNested(JsonElement rootElement)
        {
            if (rootElement.ValueKind != JsonValueKind.Object) return;

            foreach (var accProp in rootElement.EnumerateObject())
            {
                var accountId = accProp.Name;
                var inner = accProp.Value;
                if (inner.ValueKind != JsonValueKind.Object) continue;

                lock (_storeLock)
                {
                    foreach (var bookingProp in inner.EnumerateObject())
                    {
                        var bookingId = bookingProp.Name;
                        if (bookingProp.Value.ValueKind == JsonValueKind.Null)
                        {
                            _store.TryRemove(bookingId, out _);
                            continue;
                        }

                        try
                        {
                            var dto = JsonSerializer.Deserialize<BookingDto>(bookingProp.Value.GetRawText(), _jsonOptions);
                            if (dto == null) continue;
                            if (dto.id == 0 && long.TryParse(bookingId, out var parsed)) dto.id = parsed;
                            dto.accountId = dto.accountId ?? accountId;
                            _store[bookingId] = dto;
                        }
                        catch (JsonException jex)
                        {
                            _logger.LogDebug(jex, "[Repo] MergeAccountNested: parse error for {id}", bookingId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "[Repo] MergeAccountNested: unexpected error for {id}", bookingId);
                        }
                    }
                }
            }
        }

        private string BuildUrl(string? child = null)
        {
            var url = $"{_databaseUrl}/{BookingNode}";
            if (!string.IsNullOrEmpty(child)) url += $"/{child}";
            url += ".json";
            if (!string.IsNullOrEmpty(_authToken)) url += $"?auth={_authToken}";
            return url;
        }

        public async Task<IEnumerable<BookingDto>> GetAllAsync(CancellationToken ct = default)
        {
            if (_store.Count > 0) return _store.Values.ToList();
            return await FetchAndPopulateStore(ct).ConfigureAwait(false);
        }

        private async Task<IEnumerable<BookingDto>> FetchAndPopulateStore(CancellationToken ct)
        {
            var url = BuildUrl();
            try
            {
                var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(json) || json == "null")
                {
                    if (_store.Count > 0) return _store.Values.ToList();
                    lock (_storeLock) { _store.Clear(); }
                    return Array.Empty<BookingDto>();
                }

                var temp = new Dictionary<string, BookingDto>(StringComparer.Ordinal);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return Array.Empty<BookingDto>();

                foreach (var accProp in root.EnumerateObject())
                {
                    var accountId = accProp.Name;
                    var inner = accProp.Value;
                    if (inner.ValueKind != JsonValueKind.Object) continue;

                    foreach (var bookingProp in inner.EnumerateObject())
                    {
                        try
                        {
                            var dto = JsonSerializer.Deserialize<BookingDto>(bookingProp.Value.GetRawText(), _jsonOptions);
                            if (dto == null) continue;
                            if (dto.id == 0 && long.TryParse(bookingProp.Name, out var parsed)) dto.id = parsed;
                            if (string.IsNullOrEmpty(dto.accountId)) dto.accountId = accountId;
                            temp[bookingProp.Name] = dto;
                        }
                        catch (JsonException jex)
                        {
                            _logger.LogDebug(jex, "[Repo] fetch parse error for {id}", bookingProp.Name);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "[Repo] fetch unexpected error for {id}", bookingProp.Name);
                        }
                    }
                }

                lock (_storeLock)
                {
                    _store.Clear();
                    foreach (var kv in temp) _store[kv.Key] = kv.Value;
                }

                _logger.LogInformation("[Repo] Snapshot fetched and stored, entries={count}", _store.Count);
                return _store.Values.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Repo] Snapshot fetch error");
                return _store.Values.ToList();
            }
        }

        public async Task<BookingDto?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_store.TryGetValue(id, out var found)) return found;
            var all = await GetAllAsync(ct).ConfigureAwait(false);
            return all.FirstOrDefault(b => b.id.ToString() == id);
        }

        public async Task CreateAsync(BookingDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(dto.accountId)) throw new ArgumentException("accountId is required when creating booking.");
            if (dto.id == 0) dto.id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var child = $"{dto.accountId}/{dto.id}";
            var response = await _httpClient.PutAsJsonAsync(BuildUrl(child), dto, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        public async Task UpdateAsync(BookingDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(dto.accountId))
            {
                var existing = await GetByIdAsync(dto.id.ToString(), ct).ConfigureAwait(false);
                if (existing == null || string.IsNullOrEmpty(existing.accountId)) throw new ArgumentException("accountId is required to update booking.");
                dto.accountId = existing.accountId;
            }
            var child = $"{dto.accountId}/{dto.id}";
            var response = await _httpClient.PutAsJsonAsync(BuildUrl(child), dto, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteAsync(string id, CancellationToken ct = default)
        {
            var all = await GetAllAsync(ct).ConfigureAwait(false);
            var found = all.FirstOrDefault(b => b.id.ToString() == id);
            if (found == null) return;
            var child = $"{found.accountId}/{found.id}";
            var response = await _httpClient.DeleteAsync(BuildUrl(child), ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        public async Task<IEnumerable<BookingDto>> GetSnapshotAsync(CancellationToken ct = default)
        {
            // return current in-memory snapshot; if empty, attempt a snapshot fetch
            if (_store.Count > 0) return _store.Values.ToList();
            return await FetchAndPopulateStore(ct).ConfigureAwait(false);
        }

        private void RaiseRealtimeUpdated(IEnumerable<BookingDto> items)
        {
            try
            {
                RealtimeUpdated?.Invoke(this, new RealtimeUpdatedEventArgs<BookingDto>(items));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Repo] Error while raising RealtimeUpdated event");
            }
        }

        public void Dispose()
        {
            try
            {
                _cts?.Cancel();
                _cts = null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Repo] Dispose error");
            }
        }
    }
}
