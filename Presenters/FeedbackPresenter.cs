using FoodManagement.Contracts;
using FoodManagement.Models;

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

        public Task CreateItemAsync(FeedbackDto dto)
        {
            throw new NotImplementedException();
        }

        public Task UpdateItemAsync(FeedbackDto dto)
        {
            throw new NotImplementedException();
        }

        public async Task DeleteItemAsync(string id)
        {
            try
            {
                await _service.DeleteAsync(id);
                _view.ShowMessage("Xóa phản hồi thành công.");
                await LoadItemsAsync();
            }
            catch (Exception ex)
            {
                _view.ShowError($"Lỗi khi xóa: {ex.Message}");
            }
        }

        public async Task LoadItemByIdAsync(string id)
        {
            try
            {
                var feedback = await _service.GetByIdAsync(id);
                if (feedback != null)
                    _view.ShowItemDetail(feedback);
                else
                    _view.ShowMessage("Không tìm thấy phản hồi.");
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
                var feedbacks = await _service.GetAllAsync();
                _view.ShowItems(feedbacks);
            }
            catch (Exception ex)
            {
                _view.ShowError($"Lỗi khi tải danh sách: {ex.Message}");
            }
        }
    }
}
