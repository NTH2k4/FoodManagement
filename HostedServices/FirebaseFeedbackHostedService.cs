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

        public FirebaseFeedbackHostedService(
            IRealtimeRepository<FeedbackDto> repo,
            IHubContext<FeedbackHub> hub,
            ILogger<FirebaseFeedbackHostedService> logger)
        {
            _repo = repo;
            _hub = hub;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[FeedbackHostedService] Starting.");

            try
            {
                var snapshot = await _repo.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
                await _hub.Clients.All.SendAsync("FeedbacksUpdated", snapshot, cancellationToken).ConfigureAwait(false);
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
                        _logger.LogInformation("[FeedbackHostedService] Pushing {count} feedbacks to clients", items.Count());
                        await _hub.Clients.All.SendAsync("FeedbacksUpdated", items).ConfigureAwait(false);
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
                await _repo.StartListeningAsync(cancellationToken).ConfigureAwait(false);
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
    }
}
