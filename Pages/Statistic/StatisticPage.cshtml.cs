using System.Text.Json;
using FoodManagement.Contracts;
using FoodManagement.Models;
using FoodManagement.Presenters;
using FoodManagement.Services; // BookingStatisticsService
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace FoodManagement.Pages.Statistic
{
    public class StatisticPageModel : PageModel, IStatisticsView
    {
        private readonly IStatisticsService _statsService;
        private readonly ILogger<StatisticPageModel> _logger;

        public StatisticPageModel(IStatisticsService statsService, ILogger<StatisticPageModel> logger)
        {
            _statsService = statsService;
            _logger = logger;
        }

        // JSON strings passed to the client to render initial charts
        public string DailyJson { get; private set; } = "[]";
        public string MonthlyJson { get; private set; } = "[]";

        // For simple user feedback
        public string? Error { get; private set; }

        // Presenter will call these view methods
        public void ShowDaily(IEnumerable<RevenueStat> daily)
        {
            var opts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            DailyJson = JsonSerializer.Serialize(daily, opts);
        }

        public void ShowMonthly(IEnumerable<RevenueStat> monthly)
        {
            var opts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            MonthlyJson = JsonSerializer.Serialize(monthly, opts);
        }

        public void ShowError(string error)
        {
            Error = error;
        }

        public async Task OnGetAsync()
        {
            // Example: Last 30 days, and this year for monthly
            var todayUtc = DateTime.UtcNow.Date;
            var fromUtc = todayUtc.AddDays(-29); // inclusive -> 30 days total
            var toUtc = todayUtc;
            var year = DateTime.UtcNow.Year;

            var presenter = new StatisticsPresenter(_statsService, this);
            await presenter.LoadAsync(fromUtc, toUtc, year);
        }
    }
}
