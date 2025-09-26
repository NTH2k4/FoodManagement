using FoodManagement.Contracts;
using FoodManagement.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace FoodManagement.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IDashboardService _statsService;

        public IndexModel(IDashboardService statsService)
        {
            _statsService = statsService ?? throw new ArgumentNullException(nameof(statsService));
        }

        public string TodayRevenueJson { get; private set; } = "0";
        public string TopFoodsTodayJson { get; private set; } = "[]";
        public string TopFoodsMonthJson { get; private set; } = "[]";
        public string TopUsersDayJson { get; private set; } = "[]";
        public string TopUsersMonthJson { get; private set; } = "[]";

        public string TodayRevenueDisplay { get; private set; } = "0 VNĐ";
        public string? ErrorMessage { get; private set; }

        private static readonly JsonSerializerOptions _camelOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public async Task OnGetAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var todayRevenue = await _statsService.GetTodayRevenueAsync(cancellationToken).ConfigureAwait(false);
                TodayRevenueJson = JsonSerializer.Serialize(todayRevenue, _camelOptions);
                TodayRevenueDisplay = string.Format(System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:N0} VNĐ", todayRevenue);

                var topFoodsDay = await _statsService.GetTopFoodsAsync(StatisticsRange.Day, 10, cancellationToken).ConfigureAwait(false);
                TopFoodsTodayJson = JsonSerializer.Serialize(topFoodsDay ?? Enumerable.Empty<object>(), _camelOptions);

                var topFoodsMonth = await _statsService.GetTopFoodsAsync(StatisticsRange.Month, 10, cancellationToken).ConfigureAwait(false);
                TopFoodsMonthJson = JsonSerializer.Serialize(topFoodsMonth ?? Enumerable.Empty<object>(), _camelOptions);

                var topUsersDay = await _statsService.GetTopUsersAsync(StatisticsRange.Day, 10, cancellationToken).ConfigureAwait(false);
                TopUsersDayJson = JsonSerializer.Serialize(topUsersDay ?? Enumerable.Empty<object>(), _camelOptions);

                var topUsersMonth = await _statsService.GetTopUsersAsync(StatisticsRange.Month, 10, cancellationToken).ConfigureAwait(false);
                TopUsersMonthJson = JsonSerializer.Serialize(topUsersMonth ?? Enumerable.Empty<object>(), _camelOptions);
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "Yêu cầu bị huỷ.";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Không tải được số liệu: " + ex.Message;
            }
        }
    }
}
