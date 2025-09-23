namespace FoodManagement.Contracts
{
    public class RealtimeUpdatedEventArgs<T> : EventArgs
    {
        public IEnumerable<T> Items { get; }
        public RealtimeUpdatedEventArgs(IEnumerable<T> items) => Items = items;
    }

    public interface IRealtimeRepository<T>
    {
        Task StartListeningAsync(CancellationToken ct = default);
        Task StopListeningAsync(CancellationToken ct = default);
        event EventHandler<RealtimeUpdatedEventArgs<T>>? RealtimeUpdated;
        Task<IEnumerable<T>> GetSnapshotAsync(CancellationToken ct = default);
    }
}
