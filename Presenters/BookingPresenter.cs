using FoodManagement.Contracts;
using FoodManagement.Models;
using System;
using System.Threading.Tasks;

namespace FoodManagement.Presenters
{
    public class BookingPresenter : IPresenter<BookingDto>
    {
        private readonly IService<BookingDto> _service;
        private readonly IListView<BookingDto> _view;

        public BookingPresenter(IService<BookingDto> service, IListView<BookingDto> view)
        {
            _service = service;
            _view = view;
        }

        public async Task LoadItemsAsync(string? searchTerm, string? sortColumn, string? sortOrder, int page, int pageSize)
        {
            try
            {
                var (items, pagination) = await _service.QueryAsync(searchTerm, sortColumn, sortOrder, page, pageSize);
                _view.ShowItems(items ?? Array.Empty<BookingDto>());
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
                else _view.ShowMessage("Không tìm thấy đơn hàng.");
            }
            catch (Exception ex)
            {
                _view.ShowError(ex.Message);
            }
        }

        public async Task CreateItemAsync(BookingDto dto)
        {
            try
            {
                await _service.CreateAsync(dto);
                _view.ShowMessage("Tạo đơn hàng thành công.");
            }
            catch (Exception ex)
            {
                _view.ShowError(ex.Message);
            }
        }

        public async Task UpdateItemAsync(BookingDto dto)
        {
            try
            {
                await _service.UpdateAsync(dto);
                _view.ShowMessage("Cập nhật đơn hàng thành công.");
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
                _view.ShowMessage("Xóa đơn hàng thành công.");
            }
            catch (Exception ex)
            {
                _view.ShowError(ex.Message);
            }
        }

        public Task StopRealtimeAsync() => Task.CompletedTask;
    }
}
