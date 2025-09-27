namespace FoodManagement.Contracts
{
    public interface ILoginView
    {
        string Username { get; }
        string Password { get; }
        bool RememberMe { get; }
        void ShowError(string message);
        void RedirectTo(string url);
    }
}
