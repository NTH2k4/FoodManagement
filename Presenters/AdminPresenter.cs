using FoodManagement.Contracts;
using FoodManagement.Models;

namespace FoodManagement.Presenters
{
    public class AdminPresenter : IPresenter<AdminDto>
    {
        private readonly IService<AdminDto> _service;
        private readonly IListView<AdminDto> _view;

        public AdminPresenter(IService<AdminDto> service, IListView<AdminDto> view)
        {
            _service = service;
            _view = view;
        }

        public async Task LoadItemByIdAsync(string id)
        {
            try
            {
                var admin = await _service.GetByIdAsync(id);
                if (admin != null)
                    _view.ShowItemDetail(admin);
                else
                    _view.ShowMessage("Không tìm thấy quản trị viên.");
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
                var admins = await _service.GetAllAsync();
                _view.ShowItems(admins);
            }
            catch (Exception ex)
            {
                _view.ShowError($"Lỗi khi tải danh sách: {ex.Message}");
            }
        }

        public async Task CreateItemAsync(AdminDto dto)
        {
            try 
            {
                await _service.CreateAsync(dto);
                _view.ShowMessage("Thêm quản trị viên thành công.");
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
                _view.ShowMessage("Xoá quản trị viên thành công.");
                await LoadItemsAsync();
            }
            catch (Exception ex)
            {
                _view.ShowError($"Lỗi khi xoá: {ex.Message}");
            }
        }

        public async Task UpdateItemAsync(AdminDto dto)
        {
            try 
            {
                await _service.UpdateAsync(dto);
                _view.ShowMessage("Cập nhật quản trị viên thành công.");
                await LoadItemsAsync();
            }
            catch (Exception ex)
            {
                _view.ShowError($"Lỗi khi cập nhật: {ex.Message}");
            }
        }
    }
}
