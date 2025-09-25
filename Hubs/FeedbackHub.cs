using FoodManagement.Contracts;
using FoodManagement.Models;
using Microsoft.AspNetCore.SignalR;

namespace FoodManagement.Hubs
{
    public class FeedbackHub : Hub
    {
        private readonly IRealtimeRepository<FeedbackDto> _repo;

        public FeedbackHub(IRealtimeRepository<FeedbackDto> repo)
        {
            _repo = repo;
        }

        public async Task RequestSnapshot()
        {
            var items = await _repo.GetSnapshotAsync();
            await Clients.Caller.SendAsync("FeedbacksUpdated", items);
        }
    }
}
