using FoodManagement.Models;

namespace FoodManagement.Contracts.Foods
{
    public interface IFoodListView
    {
        void ShowFoods(IEnumerable<FoodDto> foods);
        void ShowFoodDetail(FoodDto food);
        void ShowMessage(string message);
        void ShowError(string error);
    }
}
