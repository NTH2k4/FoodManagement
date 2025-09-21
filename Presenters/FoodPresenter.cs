using FoodManagement.Contracts;
using FoodManagement.Models;

namespace FoodManagement.Presenters
{
    public class FoodPresenter : IPresenter<FoodDto>
    {
        private readonly IService<FoodDto> _service;
        private readonly IListView<FoodDto> _view;

        public FoodPresenter(IService<FoodDto> service, IListView<FoodDto> view)
        {
            _service = service;
            _view = view;
        }

        public async Task LoadItemsAsync()
        {
            try
            {
                var foods = await _service.GetAllAsync();
                _view.ShowItems(foods);
            }
            catch (Exception ex)
            {
                _view.ShowError($"Lỗi khi tải danh sách: {ex.Message}");
            }
        }

        public async Task LoadItemByIdAsync(string id)
        {
            try
            {
                var food = await _service.GetByIdAsync(id);
                if (food != null)
                    _view.ShowItemDetail(food);
                else
                    _view.ShowMessage("Không tìm thấy món ăn.");
            }
            catch (Exception ex)
            {
                _view.ShowError($"Lỗi khi tải chi tiết: {ex.Message}");
            }
        }

        public async Task CreateItemAsync(FoodDto dto)
        {
            try
            {
                await _service.CreateAsync(dto);
                _view.ShowMessage("Thêm món ăn thành công.");
                await LoadItemsAsync(); // reload danh sách
            }
            catch (Exception ex)
            {
                _view.ShowError($"Lỗi khi thêm: {ex.Message}");
            }
        }

        public async Task UpdateItemAsync(FoodDto dto)
        {
            try
            {
                await _service.UpdateAsync(dto);
                _view.ShowMessage("Cập nhật món ăn thành công.");
                await LoadItemsAsync();
            }
            catch (Exception ex)
            {
                _view.ShowError($"Lỗi khi cập nhật: {ex.Message}");
            }
        }

        public async Task DeleteItemAsync(string id)
        {
            try
            {
                await _service.DeleteAsync(id);
                _view.ShowMessage("Xóa món ăn thành công.");
                await LoadItemsAsync();
            }
            catch (Exception ex)
            {
                _view.ShowError($"Lỗi khi xóa: {ex.Message}");
            }
        }
    }
}
