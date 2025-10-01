using FoodManagement.Contracts;
using FoodManagement.Models;
using System;
using System.Threading.Tasks;

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

        public async Task LoadItemsAsync(string? searchTerm, string? sortColumn, string? sortOrder, int page, int pageSize)
        {
            try
            {
                var (items, pagination) = await _service.QueryAsync(searchTerm, sortColumn, sortOrder, page, pageSize);
                _view.ShowItems(items ?? Array.Empty<FoodDto>());
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
                else _view.ShowMessage("Không tìm thấy món ăn.");
            }
            catch (Exception ex)
            {
                _view.ShowError(ex.Message);
            }
        }

        public async Task CreateItemAsync(FoodDto dto)
        {
            try
            {
                await _service.CreateAsync(dto);
                _view.ShowMessage("Tạo món ăn thành công.");
            }
            catch (Exception ex)
            {
                _view.ShowError(ex.Message);
            }
        }

        public async Task UpdateItemAsync(FoodDto dto)
        {
            try
            {
                await _service.UpdateAsync(dto);
                _view.ShowMessage("Cập nhật món ăn thành công.");
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
                _view.ShowMessage("Xóa món ăn thành công.");
            }
            catch (Exception ex)
            {
                _view.ShowError(ex.Message);
            }
        }

        public Task StopRealtimeAsync() => Task.CompletedTask;
    }
}
