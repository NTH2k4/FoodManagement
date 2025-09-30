// Pages/Dashboard/IndexModel.cs
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FoodManagement.Contracts;
using FoodManagement.Models;
using FoodManagement.Presenters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodManagement.Pages.Dashboard
{
    public class IndexModel : PageModel, IDashboardView
    {
        private readonly Func<IDashboardView, DashboardPresenter> _presenterFactory;

        public IndexModel(Func<IDashboardView, DashboardPresenter> presenterFactory)
        {
            _presenterFactory = presenterFactory ?? throw new ArgumentNullException(nameof(presenterFactory));
        }

        public string TodayRevenueJson { get; private set; } = "0";
        public string TopFoodsTodayJson { get; private set; } = "[]";
        public string TopFoodsMonthJson { get; private set; } = "[]";
        public string TopUsersDayJson { get; private set; } = "[]";
        public string TopUsersMonthJson { get; private set; } = "[]";
        public string TopPaymentsDayJson { get; private set; } = "[]";
        public string TopPaymentsMonthJson { get; private set; } = "[]";

        public string TodayRevenueDisplay { get; private set; } = "0 VNĐ";
        public string? ErrorMessage { get; private set; }

        private static readonly JsonSerializerOptions _camelOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public async Task OnGetAsync(CancellationToken cancellationToken = default)
        {
            var presenter = _presenterFactory(this);
            await presenter.LoadAsync(cancellationToken).ConfigureAwait(false);
        }

        void IDashboardView.ShowTodayRevenue(decimal amount)
        {
            TodayRevenueJson = JsonSerializer.Serialize(amount, _camelOptions);
            TodayRevenueDisplay = string.Format(System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:N0} VNĐ", amount);
        }

        void IDashboardView.ShowTopFoodsDay(IEnumerable<TopFoodStat> items)
        {
            TopFoodsTodayJson = JsonSerializer.Serialize(items ?? Array.Empty<TopFoodStat>(), _camelOptions);
        }

        void IDashboardView.ShowTopFoodsMonth(IEnumerable<TopFoodStat> items)
        {
            TopFoodsMonthJson = JsonSerializer.Serialize(items ?? Array.Empty<TopFoodStat>(), _camelOptions);
        }

        void IDashboardView.ShowTopUsersDay(IEnumerable<TopUserStat> items)
        {
            TopUsersDayJson = JsonSerializer.Serialize(items ?? Array.Empty<TopUserStat>(), _camelOptions);
        }

        void IDashboardView.ShowTopUsersMonth(IEnumerable<TopUserStat> items)
        {
            TopUsersMonthJson = JsonSerializer.Serialize(items ?? Array.Empty<TopUserStat>(), _camelOptions);
        }

        void IDashboardView.ShowPreferredPaymentsDay(IEnumerable<PaymentMethodStat> items)
        {
            TopPaymentsDayJson = JsonSerializer.Serialize(items ?? Array.Empty<PaymentMethodStat>(), _camelOptions);
        }

        void IDashboardView.ShowPreferredPaymentsMonth(IEnumerable<PaymentMethodStat> items)
        {
            TopPaymentsMonthJson = JsonSerializer.Serialize(items ?? Array.Empty<PaymentMethodStat>(), _camelOptions);
        }

        void IDashboardView.ShowError(string error)
        {
            ErrorMessage = error;
        }
    }
}
