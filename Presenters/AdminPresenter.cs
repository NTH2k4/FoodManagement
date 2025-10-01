using FoodManagement.Contracts;
using FoodManagement.Models;
using System;
using System.Threading.Tasks;

namespace FoodManagement.Presenters
{
    public class AdminPresenter : IPresenter<AdminDto>
    {
        private readonly IAdminService _service;
        private readonly IListView<AdminDto> _view;

        public AdminPresenter(IAdminService service, IListView<AdminDto> view)
        {
            _service = service;
            _view = view;
        }

        public async Task LoadItemsAsync(string? searchTerm, string? sortColumn, string? sortOrder, int page, int pageSize)
        {
            try
            {
                var (items, pagination) = await _service.QueryAsync(searchTerm, sortColumn, sortOrder, page, pageSize);
                _view.ShowItems(items ?? Array.Empty<AdminDto>());
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
                else _view.ShowMessage("Không tìm thấy admin.");
            }
            catch (Exception ex)
            {
                _view.ShowError(ex.Message);
            }
        }

        public async Task CreateItemAsync(AdminDto dto)
        {
            try
            {
                await _service.CreateAsync(dto);
                _view.ShowMessage("Tạo admin thành công.");
            }
            catch (Exception ex)
            {
                _view.ShowError(ex.Message);
            }
        }

        public async Task UpdateItemAsync(AdminDto dto)
        {
            try
            {
                await _service.UpdateAsync(dto);
                _view.ShowMessage("Cập nhật admin thành công.");
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
                _view.ShowMessage("Xóa admin thành công.");
            }
            catch (Exception ex)
            {
                _view.ShowError(ex.Message);
            }
        }

        public async Task ChangePasswordAsync(string adminId, string currentPassword, string newPassword)
        {
            try
            {
                await _service.ChangePasswordAsync(adminId, currentPassword, newPassword);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }

        public Task StopRealtimeAsync() => Task.CompletedTask;
    }
}
