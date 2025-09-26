using FoodManagement.Models;

namespace FoodManagement.Contracts
{
    public interface IStatisticsService
    {
        Task<IEnumerable<RevenueStat>> GetDailyRevenueAsync(DateTime fromDateUtc, DateTime toDateUtc, CancellationToken ct = default);
        Task<IEnumerable<RevenueStat>> GetMonthlyRevenueAsync(int year, CancellationToken ct = default);
    }

    public interface IDashboardService
    {
        Task<decimal> GetTodayRevenueAsync(CancellationToken ct = default);
        Task<IEnumerable<PaymentMethodStat>> GetPreferredPaymentAsync(StatisticsRange range, CancellationToken ct = default);
        Task<IEnumerable<TopFoodStat>> GetTopFoodsAsync(StatisticsRange range, int top = 10, CancellationToken ct = default);
        Task<IEnumerable<TopUserStat>> GetTopUsersAsync(StatisticsRange range, int top = 10, CancellationToken ct = default);
        Task RefreshAsync(CancellationToken ct = default);
    }
}
