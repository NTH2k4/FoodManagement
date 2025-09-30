using System.Collections.Generic;
using FoodManagement.Models;

namespace FoodManagement.Presenters
{
    public interface IDashboardView
    {
        void ShowTodayRevenue(decimal amount);
        void ShowTopFoodsDay(IEnumerable<TopFoodStat> items);
        void ShowTopFoodsMonth(IEnumerable<TopFoodStat> items);
        void ShowTopUsersDay(IEnumerable<TopUserStat> items);
        void ShowTopUsersMonth(IEnumerable<TopUserStat> items);
        void ShowPreferredPaymentsDay(IEnumerable<PaymentMethodStat> items);
        void ShowPreferredPaymentsMonth(IEnumerable<PaymentMethodStat> items);
        void ShowError(string error);
    }
}
