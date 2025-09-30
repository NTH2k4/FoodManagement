using FoodManagement.Contracts;
using FoodManagement.Models;
using System;
using System.Threading.Tasks;

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

        public async Task LoadItemsAsync(string? searchTerm, string? sortColumn, string? sortOrder, int page, int pageSize)
        {
            try
            {
                var (items, pagination) = await _service.QueryAsync(searchTerm, sortColumn, sortOrder, page, pageSize);
                _view.ShowItems(items ?? Array.Empty<UserDto>());
                if (pagination != null) _view.SetPagination(pagination);
            }
            catch (Exception ex)
            {
                _view.ShowError(ex.Message);
            }
        }

        public async Task LoadItemByIdAsync(string id)
        {
            try
            {
                var dto = await _service.GetByIdAsync(id);
                if (dto != null) _view.ShowItemDetail(dto);
                else _view.ShowMessage("Không tìm thấy người dùng.");
            }
            catch (Exception ex)
            {
                _view.ShowError(ex.Message);
            }
        }

        public async Task CreateItemAsync(UserDto dto)
        {
            try
            {
                await _service.CreateAsync(dto);
                _view.ShowMessage("Tạo tài khoản thành công.");
                await LoadItemsAsync(null, null, null, 1, 10);
            }
            catch (Exception ex)
            {
                _view.ShowError(ex.Message);
            }
        }

        public async Task UpdateItemAsync(UserDto dto)
        {
            try
            {
                await _service.UpdateAsync(dto);
                _view.ShowMessage("Cập nhật tài khoản thành công.");
                await LoadItemsAsync(null, null, null, 1, 10);
            }
            catch (Exception ex)
            {
                _view.ShowError(ex.Message);
            }
        }

        public async Task DeleteItemAsync(string id)
        {
            try
            {
                await _service.DeleteAsync(id);
                _view.ShowMessage("Xóa người dùng thành công.");
                await LoadItemsAsync(null, null, null, 1, 10);
            }
            catch (Exception ex)
            {
                _view.ShowError(ex.Message);
            }
        }

        public Task StopRealtimeAsync() => Task.CompletedTask;
    }
}
