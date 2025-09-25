using FoodManagement.Contracts;
using FoodManagement.Hubs;
using FoodManagement.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FoodManagement.HostedServices
{
    public class FirebaseUserHostedService : IHostedService, IDisposable
    {
        private readonly IRealtimeRepository<UserDto> _repo;
        private readonly IHubContext<UserHub> _hub;
        private readonly ILogger<FirebaseUserHostedService> _logger;
        private bool _subscribed = false;

        public FirebaseUserHostedService(
            IRealtimeRepository<UserDto> repo,
            IHubContext<UserHub> hub,
            ILogger<FirebaseUserHostedService> logger)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _hub = hub ?? throw new ArgumentNullException(nameof(hub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[UserHostedService] Starting.");

            try
            {
                var snapshot = await _repo.GetSnapshotAsync(cancellationToken);
                _logger.LogInformation("[UserHostedService] Sending initial snapshot (count={count})", snapshot?.Count() ?? 0);
                await _hub.Clients.All.SendAsync("UsersUpdated", snapshot, cancellationToken);
            }
            catch (OperationCanceledException) { /* shutdown */ }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserHostedService] Error sending initial snapshot");
            }

            if (!_subscribed)
            {
                _repo.RealtimeUpdated += Repo_RealtimeUpdated;
                _subscribed = true;
            }

            try
            {
                await _repo.StartListeningAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserHostedService] Error starting repo listening");
            }
        }

        private async void Repo_RealtimeUpdated(object? sender, RealtimeUpdatedEventArgs<UserDto> e)
        {
            try
            {
                var items = e?.Items ?? Enumerable.Empty<UserDto>();
                _logger.LogDebug("[UserHostedService] RealtimeUpdated -> pushing {count} users", items.Count());
                await _hub.Clients.All.SendAsync("UsersUpdated", items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserHostedService] Error while pushing UsersUpdated");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[UserHostedService] Stopping.");

            if (_subscribed)
            {
                try
                {
                    _repo.RealtimeUpdated -= Repo_RealtimeUpdated;
                }
                catch { /* ignore */ }
                _subscribed = false;
            }

            try
            {
                await _repo.StopListeningAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[UserHostedService] Error while stopping repo listening");
            }
        }

        public void Dispose()
        {
            if (_subscribed)
            {
                try { _repo.RealtimeUpdated -= Repo_RealtimeUpdated; }
                catch { }
                _subscribed = false;
            }
        }
    }
}
