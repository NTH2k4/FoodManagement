using FoodManagement.Contracts;
using FoodManagement.Models;
using System;
using System.Threading.Tasks;

namespace FoodManagement.Presenters
{
    public class FeedbackPresenter : IPresenter<FeedbackDto>
    {
        private readonly IService<FeedbackDto> _service;
        private readonly IListView<FeedbackDto> _view;

        public FeedbackPresenter(IService<FeedbackDto> service, IListView<FeedbackDto> view)
        {
            _service = service;
            _view = view;
        }

        public async Task LoadItemsAsync(string? searchTerm, string? sortColumn, string? sortOrder, int page, int pageSize)
        {
            try
            {
                var (items, pagination) = await _service.QueryAsync(searchTerm, sortColumn, sortOrder, page, pageSize);
                _view.ShowItems(items ?? Array.Empty<FeedbackDto>());
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
                else _view.ShowMessage("Không tìm thấy phản hồi.");
            }
            catch (Exception ex)
            {
                _view.ShowError(ex.Message);
            }
        }

        public async Task CreateItemAsync(FeedbackDto dto)
        {
            try
            {
                await _service.CreateAsync(dto);
                _view.ShowMessage("Gửi phản hồi thành công.");
            }
            catch (Exception ex)
            {
                _view.ShowError(ex.Message);
            }
        }

        public async Task UpdateItemAsync(FeedbackDto dto)
        {
            try
            {
                await _service.UpdateAsync(dto);
                _view.ShowMessage("Cập nhật phản hồi thành công.");
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
                _view.ShowMessage("Xóa phản hồi thành công.");
            }
            catch (Exception ex)
            {
                _view.ShowError(ex.Message);
            }
        }

        public Task StopRealtimeAsync() => Task.CompletedTask;
    }
}
