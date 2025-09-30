namespace FoodManagement.Contracts
{
    public interface IPresenter<T>
    {
        Task LoadItemsAsync(string? searchTerm, string? sortColumn, string? sortOrder, int page, int pageSize);
        Task LoadItemByIdAsync(string id);
        Task CreateItemAsync(T dto);
        Task UpdateItemAsync(T dto);
        Task DeleteItemAsync(string id);
        Task StopRealtimeAsync();
    }
}
