using FoodManagement.Contracts;
using FoodManagement.Hubs;
using FoodManagement.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FoodManagement.HostedServices
{
    public class FirebaseAdminHostedService : IHostedService, IDisposable
    {
        private readonly IRealtimeRepository<AdminDto> _repo;
        private readonly IHubContext<AdminHub> _hub;
        private readonly ILogger<FirebaseAdminHostedService> _logger;
        private EventHandler<RealtimeUpdatedEventArgs<AdminDto>>? _handler;
        private HashSet<string> _lastIds = new(StringComparer.Ordinal);
        private readonly object _sync = new();

        public FirebaseAdminHostedService(
            IRealtimeRepository<AdminDto> repo,
            IHubContext<AdminHub> hub,
            ILogger<FirebaseAdminHostedService> logger)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _hub = hub ?? throw new ArgumentNullException(nameof(hub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[AdminHostedService] Starting.");

            try
            {
                var snapshot = await _repo.GetSnapshotAsync(cancellationToken);
                var ids = GetIds(snapshot);
                lock (_sync) { _lastIds = ids; }

                _logger.LogInformation("[AdminHostedService] Initial snapshot: {count} admin(s)", ids.Count);
                await _hub.Clients.All.SendAsync("AdminsUpdated", snapshot, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminHostedService] initial snapshot error");
            }

            _handler = (s, e) =>
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var items = e?.Items ?? Enumerable.Empty<AdminDto>();
                        var newIds = GetIds(items);

                        bool changed;
                        int added = 0, removed = 0;
                        lock (_sync)
                        {
                            changed = !_lastIds.SetEquals(newIds);
                            if (changed)
                            {
                                added = newIds.Except(_lastIds).Count();
                                removed = _lastIds.Except(newIds).Count();
                                _lastIds = newIds;
                            }
                        }

                        if (!changed) return;

                        _logger.LogInformation("[AdminHostedService] Data changed: {added} added, {removed} removed -> pushing {count} admin(s)",
                            added, removed, newIds.Count);
                        await _hub.Clients.All.SendAsync("AdminsUpdated", items);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[AdminHostedService] Error while pushing AdminsUpdated");
                    }
                });
            };

            _repo.RealtimeUpdated += _handler;

            try
            {
                await _repo.StartListeningAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminHostedService] Error starting repo listening");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[AdminHostedService] Stopping.");
            if (_handler != null)
            {
                try
                {
                    _repo.RealtimeUpdated -= _handler;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[AdminHostedService] Error while unsubscribing handler");
                }
                _handler = null;
            }

            try
            {
                await _repo.StopListeningAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[AdminHostedService] Error while stopping repo listening");
            }
        }

        public void Dispose()
        {
            if (_handler != null)
            {
                try { _repo.RealtimeUpdated -= _handler; }
                catch { }
                _handler = null;
            }
        }

        private static HashSet<string> GetIds(IEnumerable<AdminDto> items)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            if (items == null) return set;
            foreach (var it in items)
            {
                if (it == null) continue;
                var prop = it.GetType().GetProperty("id");
                var val = prop?.GetValue(it);
                var s = val?.ToString();
                if (!string.IsNullOrEmpty(s)) set.Add(s);
            }
            return set;
        }
    }
}
