using FoodManagement.Contracts;
using FoodManagement.Hubs;
using FoodManagement.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FoodManagement.HostedServices
{
    public class FirebaseFoodHostedService : IHostedService
    {
        private readonly IRealtimeRepository<FoodDto> _repo;
        private readonly IHubContext<FoodHub> _hub;
        private readonly ILogger<FirebaseFoodHostedService> _logger;
        private EventHandler<RealtimeUpdatedEventArgs<FoodDto>>? _handler;
        private HashSet<string> _lastIds = new(StringComparer.Ordinal);
        private readonly object _sync = new();

        public FirebaseFoodHostedService(IRealtimeRepository<FoodDto> repo, IHubContext<FoodHub> hub, ILogger<FirebaseFoodHostedService> logger)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _hub = hub ?? throw new ArgumentNullException(nameof(hub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[FoodHostedService] Starting.");

            try
            {
                var snapshot = await _repo.GetSnapshotAsync(cancellationToken);
                var ids = GetIds(snapshot);
                lock (_sync) { _lastIds = ids; }

                _logger.LogInformation("[FoodHostedService] Initial snapshot: {count} food(s)", ids.Count);
                await _hub.Clients.All.SendAsync("FoodsUpdated", snapshot, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FoodHostedService] initial snapshot error");
            }

            _handler = (s, e) =>
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var items = e?.Items ?? Enumerable.Empty<FoodDto>();
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

                        _logger.LogInformation("[FoodHostedService] Data changed: {added} added, {removed} removed -> pushing {count} food(s)",
                            added, removed, newIds.Count);
                        await _hub.Clients.All.SendAsync("FoodsUpdated", items);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[FoodHostedService] Error pushing to clients");
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
                _logger.LogError(ex, "[FoodHostedService] StartListeningAsync failed");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[FoodHostedService] Stopping.");
            try
            {
                if (_handler != null)
                {
                    _repo.RealtimeUpdated -= _handler;
                    _handler = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[FoodHostedService] Error while unsubscribing handler");
            }

            try
            {
                await _repo.StopListeningAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[FoodHostedService] StopListeningAsync error");
            }
        }

        private static HashSet<string> GetIds(IEnumerable<FoodDto> items)
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
