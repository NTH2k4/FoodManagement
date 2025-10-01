using FoodManagement.Contracts;
using System;
using System.Threading.Tasks;

namespace FoodManagement.Presenters.Adapters
{
    internal class CreateAdapterPresenter<T> : IPresenter<T>
    {
        private readonly IService<T> _service;
        private readonly ICreateView _view;
        public CreateAdapterPresenter(IService<T> service, ICreateView view)
        {
            _service = service;
            _view = view;
        }
        public Task LoadItemsAsync(string? searchTerm, string? sortColumn, string? sortOrder, int page, int pageSize) => Task.CompletedTask;
        public Task LoadItemByIdAsync(string id) => Task.CompletedTask;
        public async Task CreateItemAsync(T dto)
        {
            try { await _service.CreateAsync(dto); _view.ShowMessage("Tạo thành công."); }
            catch (Exception ex) { _view.ShowError(ex.Message); }
        }
        public async Task UpdateItemAsync(T dto)
        {
            try { await _service.UpdateAsync(dto); _view.ShowMessage("Cập nhật thành công."); }
            catch (Exception ex) { _view.ShowError(ex.Message); }
        }
        public async Task DeleteItemAsync(string id)
        {
            try { await _service.DeleteAsync(id); _view.ShowMessage("Xóa thành công."); }
            catch (Exception ex) { _view.ShowError(ex.Message); }
        }
        public Task StopRealtimeAsync() => Task.CompletedTask;
    }

    internal class EditAdapterPresenter<T> : IPresenter<T>
    {
        private readonly IService<T> _service;
        private readonly IEditView<T> _view;
        public EditAdapterPresenter(IService<T> service, IEditView<T> view)
        {
            _service = service;
            _view = view;
        }
        public Task LoadItemsAsync(string? searchTerm, string? sortColumn, string? sortOrder, int page, int pageSize) => Task.CompletedTask;
        public async Task LoadItemByIdAsync(string id)
        {
            try
            {
                var dto = await _service.GetByIdAsync(id);
                if (dto != null) _view.ShowItemDetail(dto);
                else _view.ShowMessage("Không tìm thấy.");
            }
            catch (Exception ex) { _view.ShowError(ex.Message); }
        }
        public async Task CreateItemAsync(T dto)
        {
            try { await _service.CreateAsync(dto); _view.ShowMessage("Tạo thành công."); }
            catch (Exception ex) { _view.ShowError(ex.Message); }
        }
        public async Task UpdateItemAsync(T dto)
        {
            try { await _service.UpdateAsync(dto); _view.ShowMessage("Cập nhật thành công."); }
            catch (Exception ex) { _view.ShowError(ex.Message); }
        }
        public async Task DeleteItemAsync(string id)
        {
            try { await _service.DeleteAsync(id); _view.ShowMessage("Xóa thành công."); }
            catch (Exception ex) { _view.ShowError(ex.Message); }
        }
        public Task StopRealtimeAsync() => Task.CompletedTask;
    }
}
