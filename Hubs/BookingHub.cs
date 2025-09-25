using Microsoft.AspNetCore.SignalR;

namespace FoodManagement.Hubs
{
    public class BookingHub : Hub
    {
        // no server methods required for now; server will push "BookingsUpdated" events
    }
}
