using FoodManagement.Models;

namespace FoodManagement.Contracts
{
    public interface IPresenter<T>
    {
        Task LoadItemsAsync();
        Task LoadItemByIdAsync(string id);
        Task CreateItemAsync(T dto);
        Task UpdateItemAsync(T dto);
        Task DeleteItemAsync(string id);
    }
}
