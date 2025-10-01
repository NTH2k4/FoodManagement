using FoodManagement.Contracts;
using FoodManagement.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace FoodManagement.Services
{
    public class StatisticsService : IStatisticsService
    {
        private readonly IRepository<BookingDto> _repo;
        private readonly ILogger<StatisticsService> _logger;

        public StatisticsService(IRepository<BookingDto> repo, ILogger<StatisticsService> logger)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<RevenueStat>> GetDailyRevenueAsync(DateTime fromDateUtc, DateTime toDateUtc, CancellationToken ct = default)
        {
            if (toDateUtc < fromDateUtc) throw new ArgumentException("toDateUtc must be >= fromDateUtc");
            var all = (await _repo.GetAllAsync().ConfigureAwait(false))?.ToList() ?? new List<BookingDto>();
            var pairs = all.Select(b =>
            {
                var created = TryGetBookingCreatedAt(b);
                var amount = TryGetBookingAmount(b);
                return new { Created = created, Amount = amount };
            })
            .Where(x => x.Created.HasValue && x.Amount.HasValue)
            .Select(x => new { Date = x.Created.Value.Date, Amount = x.Amount.Value })
            .Where(x => x.Date >= fromDateUtc.Date && x.Date <= toDateUtc.Date);

            var dict = pairs
                .GroupBy(x => x.Date)
                .OrderBy(g => g.Key)
                .Select(g => new RevenueStat
                {
                    Period = g.Key,
                    Total = g.Sum(i => i.Amount),
                    Count = g.Count()
                }).ToList();

            var results = new List<RevenueStat>();
            for (var day = fromDateUtc.Date; day <= toDateUtc.Date; day = day.AddDays(1))
            {
                var found = dict.FirstOrDefault(d => d.Period == day);
                if (found != null) results.Add(found);
                else results.Add(new RevenueStat { Period = day, Total = 0m, Count = 0 });
            }

            return results;
        }

        public async Task<IEnumerable<RevenueStat>> GetMonthlyRevenueAsync(int year, CancellationToken ct = default)
        {
            var all = (await _repo.GetAllAsync().ConfigureAwait(false))?.ToList() ?? new List<BookingDto>();
            var pairs = all.Select(b =>
            {
                var created = TryGetBookingCreatedAt(b);
                var amount = TryGetBookingAmount(b);
                return new { Created = created, Amount = amount };
            })
            .Where(x => x.Created.HasValue && x.Amount.HasValue)
            .Select(x => new { Year = x.Created.Value.Year, Month = x.Created.Value.Month, Amount = x.Amount.Value })
            .Where(x => x.Year == year);

            var grouped = pairs.GroupBy(x => x.Month)
                .OrderBy(g => g.Key)
                .Select(g => new RevenueStat
                {
                    Period = new DateTime(year, g.Key, 1),
                    Total = g.Sum(i => i.Amount),
                    Count = g.Count()
                })
                .ToList();

            var results = new List<RevenueStat>();
            for (int m = 1; m <= 12; m++)
            {
                var f = grouped.FirstOrDefault(x => x.Period.Month == m);
                if (f != null) results.Add(f);
                else results.Add(new RevenueStat { Period = new DateTime(year, m, 1), Total = 0m, Count = 0 });
            }

            return results;
        }

        private static DateTime? TryGetBookingCreatedAt(BookingDto dto)
        {
            if (dto == null) return null;
            var t = dto.GetType();
            string[] names = new[] { "createdAt", "created_at", "timestamp", "created", "date", "createdOn" };

            foreach (var n in names)
            {
                var p = t.GetProperty(n, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (p == null) continue;
                var val = p.GetValue(dto);
                if (val == null) continue;

                if (val is long l)
                {
                    if (l > 9999999999L) return DateTimeOffset.FromUnixTimeMilliseconds(l).UtcDateTime;
                    return DateTimeOffset.FromUnixTimeSeconds(l).UtcDateTime;
                }
                if (val is int ii)
                {
                    if (ii > 999999999) return DateTimeOffset.FromUnixTimeMilliseconds(ii).UtcDateTime;
                    return DateTimeOffset.FromUnixTimeSeconds(ii).UtcDateTime;
                }
                if (val is double d)
                {
                    var ll = (long)d;
                    if (ll > 9999999999L) return DateTimeOffset.FromUnixTimeMilliseconds(ll).UtcDateTime;
                    return DateTimeOffset.FromUnixTimeSeconds(ll).UtcDateTime;
                }
                if (val is DateTime dt) return dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime();
                if (val is DateTimeOffset dtoff) return dtoff.UtcDateTime;
                var s = val.ToString();
                if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
                {
                    return parsed.ToUniversalTime();
                }
                if (long.TryParse(s, out var l2))
                {
                    if (l2 > 9999999999L) return DateTimeOffset.FromUnixTimeMilliseconds(l2).UtcDateTime;
                    return DateTimeOffset.FromUnixTimeSeconds(l2).UtcDateTime;
                }
            }

            var idProp = t.GetProperty("id", BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (idProp != null)
            {
                var idVal = idProp.GetValue(dto)?.ToString();
                if (!string.IsNullOrWhiteSpace(idVal) && long.TryParse(idVal, out var lid))
                {
                    if (lid > 9999999999L) return DateTimeOffset.FromUnixTimeMilliseconds(lid).UtcDateTime;
                    if (lid > 1000000000L) return DateTimeOffset.FromUnixTimeSeconds(lid).UtcDateTime;
                }
            }

            return null;
        }

        private static decimal? TryGetBookingAmount(BookingDto dto)
        {
            if (dto == null) return null;
            var t = dto.GetType();
            string[] names = new[] { "total", "amount", "price", "totalPrice", "grandTotal", "total_amount" };

            foreach (var n in names)
            {
                var p = t.GetProperty(n, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (p == null) continue;
                var val = p.GetValue(dto);
                if (val == null) continue;

                if (val is decimal dec) return dec;
                if (val is double db) return Convert.ToDecimal(db);
                if (val is float fl) return Convert.ToDecimal(fl);
                if (val is int ii) return Convert.ToDecimal(ii);
                if (val is long ll) return Convert.ToDecimal(ll);

                var s = val.ToString();
                if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)) return parsed;
                if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out parsed)) return parsed;
            }

            return null;
        }
    }
}
