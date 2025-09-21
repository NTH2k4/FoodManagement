using FoodManagement.Contracts;
using FoodManagement.Models;

namespace FoodManagement.Presenters
{
    public class UserPresenter : IPresenter<UserDto>
    {
        private readonly IService<UserDto> _service;
        private readonly IListView<UserDto> _view;

        public UserPresenter(IService<UserDto> service, IListView<UserDto> view)
        {
            _service = service;
            _view = view;
        }

        public async Task LoadItemByIdAsync(string id)
        {
            try
            {
                var user = await _service.GetByIdAsync(id);
                if (user != null)
                    _view.ShowItemDetail(user);
                else
                    _view.ShowMessage("Không tìm thấy người dùng.");
            }
            catch (Exception ex)
            {
                _view.ShowError($"Lỗi khi tải chi tiết: {ex.Message}");
            }
        }

        public async Task LoadItemsAsync()
        {
            try
            {
                var users = await _service.GetAllAsync();
                _view.ShowItems(users);
            }
            catch (Exception ex)
            {
                _view.ShowError($"Lỗi khi tải danh sách: {ex.Message}");
            }
        }

        public async Task CreateItemAsync(UserDto dto)
        {
            try 
            {
                await _service.CreateAsync(dto);
                _view.ShowMessage("Thêm người dùng thành công.");
                await LoadItemsAsync();
            }
            catch (Exception ex)
            {
                _view.ShowError($"Lỗi khi thêm: {ex.Message}");
            }
        }

        public async Task DeleteItemAsync(string id)
        {
            try 
            {
                await _service.DeleteAsync(id);
                _view.ShowMessage("Xóa người dùng thành công.");
                await LoadItemsAsync();
            }
            catch (Exception ex)
            {
                _view.ShowError($"Lỗi khi xóa: {ex.Message}");
            }
        }

        public async Task UpdateItemAsync(UserDto dto)
        {
            try 
            {
                await _service.UpdateAsync(dto);
                _view.ShowMessage("Cập nhật người dùng thành công.");
                await LoadItemsAsync();
            }
            catch (Exception ex)
            {
                _view.ShowError($"Lỗi khi cập nhật: {ex.Message}");
            }
        }
    }
}
