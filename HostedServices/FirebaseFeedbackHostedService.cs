using FoodManagement.Contracts;
using FoodManagement.Hubs;
using FoodManagement.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FoodManagement.HostedServices
{
    public class FirebaseFeedbackHostedService : IHostedService
    {
        private readonly IRealtimeRepository<FeedbackDto> _repo;
        private readonly IHubContext<FeedbackHub> _hub;
        private readonly ILogger<FirebaseFeedbackHostedService> _logger;
        private EventHandler<RealtimeUpdatedEventArgs<FeedbackDto>>? _handler;
        private HashSet<string> _lastIds = new(StringComparer.Ordinal);
        private readonly object _sync = new();

        public FirebaseFeedbackHostedService(
            IRealtimeRepository<FeedbackDto> repo,
            IHubContext<FeedbackHub> hub,
            ILogger<FirebaseFeedbackHostedService> logger)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _hub = hub ?? throw new ArgumentNullException(nameof(hub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[FeedbackHostedService] Starting.");

            try
            {
                var snapshot = await _repo.GetSnapshotAsync(cancellationToken);
                var ids = GetIds(snapshot);
                lock (_sync) { _lastIds = ids; }

                _logger.LogInformation("[FeedbackHostedService] Initial snapshot: {count} feedback(s)", ids.Count);
                await _hub.Clients.All.SendAsync("FeedbacksUpdated", snapshot, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FeedbackHostedService] initial snapshot error");
            }

            _handler = (sender, args) =>
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var items = args?.Items ?? Enumerable.Empty<FeedbackDto>();
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

                        if (!changed)
                        {
                            // nothing changed -> no log, no push
                            return;
                        }

                        _logger.LogInformation("[FeedbackHostedService] Data changed: {added} added, {removed} removed -> pushing {count} feedback(s)",
                            added, removed, newIds.Count);
                        await _hub.Clients.All.SendAsync("FeedbacksUpdated", items);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[FeedbackHostedService] Error pushing to clients");
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
                _logger.LogError(ex, "[FeedbackHostedService] StartListeningAsync failed");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[FeedbackHostedService] Stopping.");

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
                _logger.LogWarning(ex, "[FeedbackHostedService] Error while unsubscribing handler");
            }

            try
            {
                await _repo.StopListeningAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[FeedbackHostedService] StopListeningAsync error");
            }
        }

        private static HashSet<string> GetIds(IEnumerable<FeedbackDto> items)
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
