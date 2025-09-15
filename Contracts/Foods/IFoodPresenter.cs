using FoodManagement.Models;

namespace FoodManagement.Contracts.Foods
{
    public interface IFoodPresenter
    {
        Task LoadFoodsAsync();
        Task LoadFoodByIdAsync(string id);
        Task CreateFoodAsync(FoodDto dto);
        Task UpdateFoodAsync(FoodDto dto);
        Task DeleteFoodAsync(string id);
    }
}
