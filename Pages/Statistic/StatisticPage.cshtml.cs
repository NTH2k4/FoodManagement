using System;
using System.Text.Json;
using System.Threading.Tasks;
using FoodManagement.Contracts;
using FoodManagement.Models;
using FoodManagement.Presenters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace FoodManagement.Pages.Statistic
{
    public class StatisticPageModel : PageModel, IStatisticsView
    {
        private readonly Func<IStatisticsView, StatisticsPresenter> _presenterFactory;
        private readonly ILogger<StatisticPageModel> _logger;

        public StatisticPageModel(Func<IStatisticsView, StatisticsPresenter> presenterFactory, ILogger<StatisticPageModel> logger)
        {
            _presenterFactory = presenterFactory ?? throw new ArgumentNullException(nameof(presenterFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string DailyJson { get; private set; } = "[]";
        public string MonthlyJson { get; private set; } = "[]";
        public string? Error { get; private set; }

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
            var todayUtc = DateTime.UtcNow.Date;
            var fromUtc = todayUtc.AddDays(-29);
            var toUtc = todayUtc;
            var year = DateTime.UtcNow.Year;
            var presenter = _presenterFactory(this);
            await presenter.LoadAsync(fromUtc, toUtc, year);
        }
    }
}
