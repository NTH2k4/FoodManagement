using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FoodManagement.Repositories.Infrastructure
{
    public class FirebaseSseEvent
    {
        public string Event { get; init; } = "message";
        public string Path { get; init; } = "/";
        public string RawData { get; init; } = "null";
    }

    public class FirebaseRealtimeSseListener
    {
        private readonly HttpClient _httpClient;
        private readonly string _streamUrl;
        private readonly TimeSpan _reconnectDelay;
        private readonly ILogger<FirebaseRealtimeSseListener>? _logger;

        public event Action<FirebaseSseEvent>? OnMessage;

        public FirebaseRealtimeSseListener(string databaseUrl, string node, string? authToken = null, ILogger<FirebaseRealtimeSseListener>? logger = null, TimeSpan? reconnectDelay = null)
        {
            _logger = logger;
            var baseUrl = (databaseUrl ?? string.Empty).TrimEnd('/');
            var nodePart = (node ?? string.Empty).Trim('/');
            _streamUrl = $"{baseUrl}/{nodePart}.json?print=event-stream";
            if (!string.IsNullOrEmpty(authToken))
            {
                _streamUrl += $"&auth={Uri.EscapeDataString(authToken)}";
            }
            _httpClient = new HttpClient() { Timeout = Timeout.InfiniteTimeSpan };
            _reconnectDelay = reconnectDelay ?? TimeSpan.FromSeconds(5);
        }

        public async Task StartAsync(CancellationToken ct = default)
        {
            _logger?.LogInformation("[SSE] Start listening to {url}", _streamUrl);
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    using var req = new HttpRequestMessage(HttpMethod.Get, _streamUrl);
                    req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

                    using var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                    resp.EnsureSuccessStatusCode();

                    using var stream = await resp.Content.ReadAsStreamAsync(ct);
                    using var reader = new StreamReader(stream, Encoding.UTF8);

                    string? eventName = null;
                    var dataBuilder = new StringBuilder();

                    _logger?.LogInformation("[SSE] Connected, reading stream...");

                    while (!ct.IsCancellationRequested && !reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        if (line is null) break;

                        if (line.StartsWith("event:"))
                        {
                            eventName = line.Substring("event:".Length).Trim();
                        }
                        else if (line.StartsWith("data:"))
                        {
                            var part = line.Substring("data:".Length);
                            if (dataBuilder.Length > 0) dataBuilder.Append('\n');
                            dataBuilder.Append(part.Trim());
                        }
                        else if (string.IsNullOrWhiteSpace(line))
                        {
                            if (dataBuilder.Length > 0)
                            {
                                ProcessBlock(eventName ?? "message", dataBuilder.ToString());
                            }
                            eventName = null;
                            dataBuilder.Clear();
                        }
                        // ignore other prefixes
                    }

                    _logger?.LogWarning("[SSE] Stream reader ended, reconnecting...");
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "[SSE] Error while listening, reconnect after {delay}s", _reconnectDelay.TotalSeconds);
                    try { await Task.Delay(_reconnectDelay, ct); } catch { break; }
                }
            }
            _logger?.LogInformation("[SSE] Listener stopped.");
        }

        private void ProcessBlock(string eventName, string dataLine)
        {
            try
            {
                using var doc = JsonDocument.Parse(dataLine);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("path", out var pathEl) && root.TryGetProperty("data", out var dataEl))
                {
                    var path = pathEl.ValueKind == JsonValueKind.String ? pathEl.GetString() ?? "/" : "/";
                    var rawData = dataEl.GetRawText();
                    var evt = new FirebaseSseEvent { Event = eventName, Path = path, RawData = rawData };
                    OnMessage?.Invoke(evt);
                    return;
                }

                var raw = root.GetRawText();
                var evt2 = new FirebaseSseEvent { Event = eventName, Path = "/", RawData = raw };
                OnMessage?.Invoke(evt2);
            }
            catch (JsonException)
            {
                var evt = new FirebaseSseEvent { Event = eventName, Path = "/", RawData = dataLine };
                OnMessage?.Invoke(evt);
            }
        }
    }
}
