namespace FoodManagement.Contracts
{
    public interface ICreateView
    {
        void ShowMessage(string message);
        void ShowError(string error);
        void ShowValidationErrors(IDictionary<string, string> fieldErrors);
        Task RedirectToListAsync();
    }
}
