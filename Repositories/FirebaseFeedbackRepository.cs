using FoodManagement.Contracts;
using FoodManagement.Models;
using FoodManagement.Repositories.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace FoodManagement.Repositories
{
    public class FirebaseFeedbackRepository : IRepository<FeedbackDto>, IRealtimeRepository<FeedbackDto>, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _databaseUrl;
        private readonly string? _authToken;
        private const string FeedbackNode = "feedback";
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly FirebaseRealtimeSseListener _sseListener;
        private readonly ConcurrentDictionary<string, FeedbackDto> _store = new(StringComparer.Ordinal);
        private readonly object _storeLock = new();
        private CancellationTokenSource? _cts;
        private Task? _listenTask;
        private DateTime _lastPush = DateTime.UtcNow;
        private readonly ILogger<FirebaseFeedbackRepository> _logger;

        public event EventHandler<RealtimeUpdatedEventArgs<FeedbackDto>>? RealtimeUpdated;

        public FirebaseFeedbackRepository(IConfiguration configuration, ILogger<FirebaseFeedbackRepository> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _databaseUrl = configuration["Firebase:DatabaseUrl"]?.TrimEnd('/') ?? throw new InvalidOperationException("DatabaseUrl not configured");
            _authToken = configuration["Firebase:AuthToken"];

            _sseListener = new FirebaseRealtimeSseListener(_databaseUrl, FeedbackNode, _authToken, logger: logger as ILogger<FirebaseRealtimeSseListener>);
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
                catch (OperationCanceledException) { /* normal on shutdown */ }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[FeedbackRepo] SSE listener failed");
                }
            }, _cts.Token);

            // watchdog fallback snapshot fetch
            _ = Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), _cts.Token).ConfigureAwait(false);
                        if (DateTime.UtcNow - _lastPush > TimeSpan.FromSeconds(30))
                        {
                            _logger.LogInformation("[FeedbackRepo] No SSE push seen recently, performing snapshot fetch (fallback).");
                            var all = await FetchAndPopulateStore(_cts.Token).ConfigureAwait(false);
                            RaiseRealtimeUpdated(all);
                            _lastPush = DateTime.UtcNow;
                        }
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex) { _logger.LogError(ex, "[FeedbackRepo] Watchdog error"); }
                }
            }, _cts.Token);

            _logger.LogInformation("[FeedbackRepo] Listening started.");
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
                _logger.LogWarning(ex, "[FeedbackRepo] StopListeningAsync error");
            }
            return Task.CompletedTask;
        }

        private void MarkPush() => _lastPush = DateTime.UtcNow;

        private void HandleSseEvent(FirebaseSseEvent evt)
        {
            try
            {
                MarkPush();
                var path = (evt.Path ?? "/").Trim();
                if (string.IsNullOrEmpty(path)) path = "/";

                if (path == "/")
                {
                    UpdateFullTree(evt.RawData);
                    RaiseRealtimeUpdated(_store.Values.ToList());
                    return;
                }

                var segs = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segs.Length >= 1)
                {
                    var feedbackId = segs[0];
                    if (evt.RawData == "null")
                    {
                        _store.TryRemove(feedbackId, out _);
                    }
                    else
                    {
                        var dto = JsonSerializer.Deserialize<FeedbackDto>(evt.RawData, _jsonOptions);
                        if (dto != null)
                        {
                            dto.id = feedbackId;
                            _store[feedbackId] = dto;
                        }
                    }

                    RaiseRealtimeUpdated(_store.Values.ToList());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FeedbackRepo] HandleSseEvent error");
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
                        //_logger.LogWarning("[FeedbackRepo] Received root=null from Firebase SSE but local store has {count} items. Ignoring transient null.", _store.Count);
                        return;
                    }
                    lock (_storeLock) { _store.Clear(); }
                    return;
                }

                using var doc = JsonDocument.Parse(rawData);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return;

                var temp = new Dictionary<string, FeedbackDto>(StringComparer.Ordinal);
                foreach (var prop in root.EnumerateObject())
                {
                    try
                    {
                        var id = prop.Name;
                        var dto = JsonSerializer.Deserialize<FeedbackDto>(prop.Value.GetRawText(), _jsonOptions);
                        if (dto == null) continue;
                        dto.id = id;
                        temp[id] = dto;
                    }
                    catch (JsonException jex)
                    {
                        _logger.LogDebug(jex, "[FeedbackRepo] UpdateFullTree parse error for {id}", prop.Name);
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
                _logger.LogError(ex, "[FeedbackRepo] Full tree parse error");
            }
        }

        private string BuildUrl(string? child = null)
        {
            var url = $"{_databaseUrl}/{FeedbackNode}";
            if (!string.IsNullOrEmpty(child)) url += $"/{child}";
            url += ".json";
            if (!string.IsNullOrEmpty(_authToken)) url += $"?auth={_authToken}";
            return url;
        }

        public async Task<IEnumerable<FeedbackDto>> GetAllAsync(CancellationToken ct = default)
        {
            if (_store.Count > 0) return _store.Values.ToList();
            return await FetchAndPopulateStore(ct).ConfigureAwait(false);
        }

        private async Task<IEnumerable<FeedbackDto>> FetchAndPopulateStore(CancellationToken ct)
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
                    return Array.Empty<FeedbackDto>();
                }

                var temp = new Dictionary<string, FeedbackDto>(StringComparer.Ordinal);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return Array.Empty<FeedbackDto>();

                foreach (var prop in root.EnumerateObject())
                {
                    try
                    {
                        var id = prop.Name;
                        var dto = JsonSerializer.Deserialize<FeedbackDto>(prop.Value.GetRawText(), _jsonOptions);
                        if (dto == null) continue;
                        dto.id = id;
                        temp[id] = dto;
                    }
                    catch (JsonException jex)
                    {
                        _logger.LogDebug(jex, "[FeedbackRepo] fetch parse error for {id}", prop.Name);
                    }
                }

                lock (_storeLock)
                {
                    _store.Clear();
                    foreach (var kv in temp) _store[kv.Key] = kv.Value;
                }

                _logger.LogInformation("[FeedbackRepo] Snapshot fetched and stored, entries={count}", _store.Count);
                return _store.Values.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FeedbackRepo] Snapshot fetch error");
                return _store.Values.ToList();
            }
        }

        public async Task<FeedbackDto?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_store.TryGetValue(id, out var found)) return found;
            var all = await GetAllAsync(ct).ConfigureAwait(false);
            return all.FirstOrDefault(f => f.id == id);
        }

        public async Task CreateAsync(FeedbackDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(dto.id)) dto.id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            var child = dto.id;
            var response = await _httpClient.PutAsJsonAsync(BuildUrl(child), dto, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        public async Task UpdateAsync(FeedbackDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(dto.id)) throw new ArgumentException("id is required to update feedback.");
            var child = dto.id;
            var response = await _httpClient.PutAsJsonAsync(BuildUrl(child), dto, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteAsync(string id, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(id)) return;
            var response = await _httpClient.DeleteAsync(BuildUrl(id), ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        public async Task<IEnumerable<FeedbackDto>> GetSnapshotAsync(CancellationToken ct = default)
        {
            if (_store.Count > 0) return _store.Values.ToList();
            return await FetchAndPopulateStore(ct).ConfigureAwait(false);
        }

        private void RaiseRealtimeUpdated(IEnumerable<FeedbackDto> items)
        {
            try
            {
                RealtimeUpdated?.Invoke(this, new RealtimeUpdatedEventArgs<FeedbackDto>(items));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FeedbackRepo] Error while raising RealtimeUpdated");
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
                _logger.LogWarning(ex, "[FeedbackRepo] Dispose error");
            }
        }
    }
}
