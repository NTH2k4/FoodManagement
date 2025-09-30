using FoodManagement.Models;

namespace FoodManagement.Contracts
{
    public interface IListView<T>
    {
        void ShowItems(IEnumerable<T> items);
        void ShowItemDetail(T item);
        void ShowMessage(string message);
        void ShowError(string error);
        void SetPagination(PaginationInfo pagination);
    }
}
