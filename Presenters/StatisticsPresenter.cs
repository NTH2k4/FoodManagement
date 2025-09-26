using System;
using System.Threading;
using System.Threading.Tasks;
using FoodManagement.Contracts;
using FoodManagement.Models;
using FoodManagement.Services; // nơi bạn để BookingStatisticsService
using Microsoft.Extensions.Logging;

namespace FoodManagement.Presenters
{
    public interface IStatisticsView
    {
        void ShowDaily(IEnumerable<RevenueStat> daily);
        void ShowMonthly(IEnumerable<RevenueStat> monthly);
        void ShowError(string error);
    }

    public class StatisticsPresenter
    {
        private readonly IStatisticsService _svc;
        private readonly IStatisticsView _view;
        private readonly ILogger<StatisticsPresenter> _logger;

        public StatisticsPresenter(IStatisticsService svc, IStatisticsView view, ILogger<StatisticsPresenter>? logger = null)
        {
            _svc = svc ?? throw new ArgumentNullException(nameof(svc));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<StatisticsPresenter>.Instance;
        }

        public async Task LoadAsync(DateTime fromUtc, DateTime toUtc, int year, CancellationToken ct = default)
        {
            try
            {
                var daily = await _svc.GetDailyRevenueAsync(fromUtc, toUtc, ct).ConfigureAwait(false);
                var monthly = await _svc.GetMonthlyRevenueAsync(year, ct).ConfigureAwait(false);
                _view.ShowDaily(daily);
                _view.ShowMonthly(monthly);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load statistics");
                _view.ShowError($"Lỗi khi tải thống kê: {ex.Message}");
            }
        }
    }
}
