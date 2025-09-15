using FoodManagement.Contracts.Foods;
using FoodManagement.Models;

namespace FoodManagement.Presenters.Foods
{
    public class FoodPresenter : IFoodPresenter
    {
        private readonly IFoodService _service;
        private readonly IFoodListView _view;

        public FoodPresenter(IFoodService service, IFoodListView view)
        {
            _service = service;
            _view = view;
        }

        public async Task LoadFoodsAsync()
        {
            try
            {
                var foods = await _service.GetAllAsync();
                _view.ShowFoods(foods);
            }
            catch (Exception ex)
            {
                _view.ShowError($"Lỗi khi tải danh sách: {ex.Message}");
            }
        }

        public async Task LoadFoodByIdAsync(string id)
        {
            try
            {
                var food = await _service.GetByIdAsync(id);
                if (food != null)
                    _view.ShowFoodDetail(food);
                else
                    _view.ShowMessage("Không tìm thấy món ăn.");
            }
            catch (Exception ex)
            {
                _view.ShowError($"Lỗi khi tải chi tiết: {ex.Message}");
            }
        }

        public async Task CreateFoodAsync(FoodDto dto)
        {
            try
            {
                await _service.CreateAsync(dto);
                _view.ShowMessage("Thêm món ăn thành công.");
                await LoadFoodsAsync(); // reload danh sách
            }
            catch (Exception ex)
            {
                _view.ShowError($"Lỗi khi thêm: {ex.Message}");
            }
        }

        public async Task UpdateFoodAsync(FoodDto dto)
        {
            try
            {
                await _service.UpdateAsync(dto);
                _view.ShowMessage("Cập nhật món ăn thành công.");
                await LoadFoodsAsync();
            }
            catch (Exception ex)
            {
                _view.ShowError($"Lỗi khi cập nhật: {ex.Message}");
            }
        }

        public async Task DeleteFoodAsync(string id)
        {
            try
            {
                await _service.DeleteAsync(id);
                _view.ShowMessage("Xóa món ăn thành công.");
                await LoadFoodsAsync();
            }
            catch (Exception ex)
            {
                _view.ShowError($"Lỗi khi xóa: {ex.Message}");
            }
        }
    }
}
