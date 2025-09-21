using FoodManagement.Contracts;
using FoodManagement.Models;

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

        public async Task LoadItemByIdAsync(string id)
        {
            try
            {
                var booking = await _service.GetByIdAsync(id);
                if (booking != null)
                    _view.ShowItemDetail(booking);
                else
                    _view.ShowMessage("Không tìm thấy đặt hàng.");
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
                var bookings = await _service.GetAllAsync();
                _view.ShowItems(bookings);
            }
            catch (Exception ex)
            {
                _view.ShowError($"Lỗi khi tải danh sách: {ex.Message}");
            }
        }

        public async Task CreateItemAsync(BookingDto dto)
        {
            try 
            {
                await _service.CreateAsync(dto);
                _view.ShowMessage("Thêm đặt hàng thành công.");
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
                _view.ShowMessage("Xóa đặt hàng thành công.");
                await LoadItemsAsync();
            }
            catch (Exception ex)
            {
                _view.ShowError($"Lỗi khi xóa: {ex.Message}");
            }
        }

        public async Task UpdateItemAsync(BookingDto dto)
        {
            try
            {
                await _service.UpdateAsync(dto);
                _view.ShowMessage("Cập nhật đặt hàng thành công.");
                await LoadItemsAsync();
            }
            catch (Exception ex)
            {
                _view.ShowError($"Lỗi khi cập nhật: {ex.Message}");
            }
        }
    }
}
