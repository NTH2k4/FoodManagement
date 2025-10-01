using System.Collections.Generic;
using System.Threading.Tasks;
using FoodManagement.Models;

namespace FoodManagement.Contracts
{
    public interface IEditView<T>
    {
        void ShowItemDetail(T item);
        void ShowMessage(string message);
        void ShowError(string error);
        void ShowValidationErrors(IDictionary<string, string> fieldErrors);
        Task RedirectToListAsync();
    }
}
