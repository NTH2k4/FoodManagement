using FoodManagement.Contracts;
using FoodManagement.Hubs;
using FoodManagement.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FoodManagement.HostedServices
{
    public class FirebaseBookingHostedService : IHostedService
    {
        private readonly IRealtimeRepository<BookingDto> _repo;
        private readonly IHubContext<BookingHub> _hub;
        private readonly ILogger<FirebaseBookingHostedService> _logger;
        private EventHandler<RealtimeUpdatedEventArgs<BookingDto>>? _handler;
        private HashSet<string> _lastIds = new(StringComparer.Ordinal);
        private readonly object _sync = new();

        public FirebaseBookingHostedService(
            IRealtimeRepository<BookingDto> repo,
            IHubContext<BookingHub> hub,
            ILogger<FirebaseBookingHostedService> logger)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _hub = hub ?? throw new ArgumentNullException(nameof(hub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[BookingHostedService] Starting.");

            try
            {
                var snapshot = await _repo.GetSnapshotAsync(cancellationToken);
                var ids = GetIds(snapshot);
                lock (_sync) { _lastIds = ids; }

                _logger.LogInformation("[BookingHostedService] Initial snapshot: {count} booking(s)", ids.Count);
                await _hub.Clients.All.SendAsync("BookingsUpdated", snapshot, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BookingHostedService] initial snapshot error");
            }

            _handler = (sender, args) =>
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var items = args?.Items ?? Enumerable.Empty<BookingDto>();
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

                        _logger.LogInformation("[BookingHostedService] Data changed: {added} added, {removed} removed -> pushing {count} booking(s)",
                            added, removed, newIds.Count);
                        await _hub.Clients.All.SendAsync("BookingsUpdated", items);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[BookingHostedService] Error pushing to clients");
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
                _logger.LogError(ex, "[BookingHostedService] StartListeningAsync failed");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[BookingHostedService] Stopping.");

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
                _logger.LogWarning(ex, "[BookingHostedService] Error while unsubscribing handler");
            }

            try
            {
                await _repo.StopListeningAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[BookingHostedService] StopListeningAsync error");
            }
        }

        private static HashSet<string> GetIds(IEnumerable<BookingDto> items)
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
