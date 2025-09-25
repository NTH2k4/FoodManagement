using FoodManagement.Contracts;
using FoodManagement.Hubs;
using FoodManagement.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;

namespace FoodManagement.HostedServices
{
    public class FirebaseFoodHostedService : IHostedService
    {
        private readonly IRealtimeRepository<FoodDto> _repo;
        private readonly IHubContext<FoodHub> _hub;
        private readonly ILogger<FirebaseFoodHostedService> _logger;

        public FirebaseFoodHostedService(IRealtimeRepository<FoodDto> repo, IHubContext<FoodHub> hub, ILogger<FirebaseFoodHostedService> logger)
        {
            _repo = repo;
            _hub = hub;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[FoodHostedService] Starting. Subscribing to repo events.");

            // initial snapshot push
            try
            {
                var snapshot = await _repo.GetSnapshotAsync(cancellationToken);
                await _hub.Clients.All.SendAsync("FoodsUpdated", snapshot, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FoodHostedService] initial snapshot error");
            }

            // subscribe to subsequent updates
            _repo.RealtimeUpdated += async (s, e) =>
            {
                try
                {
                    var items = e.Items ?? Enumerable.Empty<FoodDto>();
                    _logger.LogInformation("[FoodHostedService] Pushing {count} foods to clients", items.Count());
                    await _hub.Clients.All.SendAsync("FoodsUpdated", items);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[FoodHostedService] Error pushing to clients");
                }
            };

            await _repo.StartListeningAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[FoodHostedService] Stopping.");
            _repo.StopListeningAsync(cancellationToken).GetAwaiter().GetResult();
            return Task.CompletedTask;
        }
    }
}
