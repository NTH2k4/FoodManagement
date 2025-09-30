using System;
using System.Threading;
using System.Threading.Tasks;
using FoodManagement.Contracts;
using FoodManagement.Models;
using Microsoft.Extensions.Logging;

namespace FoodManagement.Presenters
{
    public class DashboardPresenter
    {
        private readonly IDashboardService _svc;
        private readonly IDashboardView _view;
        private readonly ILogger<DashboardPresenter> _logger;

        public DashboardPresenter(IDashboardService svc, IDashboardView view, ILogger<DashboardPresenter>? logger = null)
        {
            _svc = svc ?? throw new ArgumentNullException(nameof(svc));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<DashboardPresenter>.Instance;
        }

        public async Task LoadAsync(CancellationToken ct = default)
        {
            try
            {
                var todayTask = _svc.GetTodayRevenueAsync(ct);
                var topFoodsDayTask = _svc.GetTopFoodsAsync(StatisticsRange.Day, 10, ct);
                var topFoodsMonthTask = _svc.GetTopFoodsAsync(StatisticsRange.Month, 10, ct);
                var topUsersDayTask = _svc.GetTopUsersAsync(StatisticsRange.Day, 10, ct);
                var topUsersMonthTask = _svc.GetTopUsersAsync(StatisticsRange.Month, 10, ct);
                var payDayTask = _svc.GetPreferredPaymentAsync(StatisticsRange.Day, ct);
                var payMonthTask = _svc.GetPreferredPaymentAsync(StatisticsRange.Month, ct);

                await Task.WhenAll(todayTask, topFoodsDayTask, topFoodsMonthTask, topUsersDayTask, topUsersMonthTask, payDayTask, payMonthTask).ConfigureAwait(false);

                _view.ShowTodayRevenue(await todayTask.ConfigureAwait(false));
                _view.ShowTopFoodsDay(await topFoodsDayTask.ConfigureAwait(false));
                _view.ShowTopFoodsMonth(await topFoodsMonthTask.ConfigureAwait(false));
                _view.ShowTopUsersDay(await topUsersDayTask.ConfigureAwait(false));
                _view.ShowTopUsersMonth(await topUsersMonthTask.ConfigureAwait(false));
                _view.ShowPreferredPaymentsDay(await payDayTask.ConfigureAwait(false));
                _view.ShowPreferredPaymentsMonth(await payMonthTask.ConfigureAwait(false));
            }
            catch (OperationCanceledException)
            {
                _view.ShowError("Yêu cầu tải dashboard bị huỷ.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while loading dashboard data");
                _view.ShowError("Không tải được dữ liệu dashboard: " + ex.Message);
            }
        }
    }
}
