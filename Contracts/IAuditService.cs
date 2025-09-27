namespace FoodManagement.Contracts
{
    public interface IAuditService
    {
        Task LogAsync(string? adminId, string action, string? details = null, CancellationToken ct = default);
    }
}
