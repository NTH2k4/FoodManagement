using FoodManagement.Models;

namespace FoodManagement.Contracts
{
    public interface IStatisticsService
    {
        Task<IEnumerable<RevenueStat>> GetDailyRevenueAsync(DateTime fromDateUtc, DateTime toDateUtc, CancellationToken ct = default);
        Task<IEnumerable<RevenueStat>> GetMonthlyRevenueAsync(int year, CancellationToken ct = default);
    }
}
