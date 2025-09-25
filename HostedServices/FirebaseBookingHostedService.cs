using FoodManagement.Contracts;
using FoodManagement.Hubs;
using FoodManagement.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace FoodManagement.HostedServices
{
    public class FirebaseBookingHostedService : IHostedService
    {
        private readonly IRealtimeRepository<BookingDto> _repo;
        private readonly IHubContext<BookingHub> _hub;
        private readonly ILogger<FirebaseBookingHostedService> _logger;
        private EventHandler<RealtimeUpdatedEventArgs<BookingDto>>? _handler;

        public FirebaseBookingHostedService(
            IRealtimeRepository<BookingDto> repo,
            IHubContext<BookingHub> hub,
            ILogger<FirebaseBookingHostedService> logger)
        {
            _repo = repo;
            _hub = hub;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[HostedService] Starting. Subscribing to repo events.");

            // initial snapshot push
            try
            {
                var snapshot = await _repo.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
                await _hub.Clients.All.SendAsync("BookingsUpdated", snapshot, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HostedService] initial snapshot error");
            }

            // subscribe - store handler so we can unsubscribe later
            _handler = (sender, args) =>
            {
                // Fire-and-forget but catch/log exceptions
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var items = args?.Items ?? Enumerable.Empty<BookingDto>();
                        _logger.LogInformation("[HostedService] Pushing {count} bookings to clients", items.Count());
                        await _hub.Clients.All.SendAsync("BookingsUpdated", items).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[HostedService] Error pushing to clients");
                    }
                });
            };

            _repo.RealtimeUpdated += _handler;

            // start listening (non-blocking)
            try
            {
                await _repo.StartListeningAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HostedService] StartListeningAsync failed");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[HostedService] Stopping.");

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
                _logger.LogWarning(ex, "[HostedService] Error while unsubscribing handler");
            }

            try
            {
                await _repo.StopListeningAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[HostedService] StopListeningAsync error");
            }
        }
    }
}
