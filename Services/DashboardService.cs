using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FoodManagement.Contracts;
using FoodManagement.Models;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace FoodManagement.Services
{
    public class DashboardService : IDashboardService, IDisposable
    {
        private readonly IRealtimeRepository<BookingDto> _bookingRepo;
        private readonly ILogger<DashboardService> _logger;
        private readonly object _sync = new();
        private IEnumerable<BookingDto> _snapshot = Enumerable.Empty<BookingDto>();
        private decimal _cachedTodayRevenue = 0m;
        private List<PaymentMethodStat> _cachedPaymentsDay = new();
        private List<PaymentMethodStat> _cachedPaymentsMonth = new();
        private List<TopFoodStat> _cachedTopFoodsDay = new();
        private List<TopFoodStat> _cachedTopFoodsMonth = new();
        private List<TopUserStat> _cachedTopUsersDay = new();
        private List<TopUserStat> _cachedTopUsersMonth = new();
        private EventHandler<RealtimeUpdatedEventArgs<BookingDto>>? _handler;
        private bool _disposed;
        private Task? _initializationTask;
        private static readonly Regex _lineItemRegex = new Regex(@"^\s*-?\s*(.+?)\s*\(\s*([\d\.,]+)\s*VNĐ\s*\)\s*(?:[-–—]\s*)?(?:Số lượng\s*:?\s*(\d+))?", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public DashboardService(IRealtimeRepository<BookingDto> bookingRepo, ILogger<DashboardService> logger)
        {
            _bookingRepo = bookingRepo ?? throw new ArgumentNullException(nameof(bookingRepo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _handler = (s, e) =>
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await RefreshAsync(CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error refreshing statistics after realtime update");
                    }
                });
            };
            _bookingRepo.RealtimeUpdated += _handler;
            _initializationTask = RefreshAsync(CancellationToken.None);
        }

        private async Task EnsureInitializedAsync(CancellationToken ct)
        {
            var init = _initializationTask;
            if (init == null)
            {
                lock (_sync)
                {
                    if (_initializationTask == null) _initializationTask = RefreshAsync(ct);
                    init = _initializationTask;
                }
            }
            try
            {
                await init!.WaitAsync(TimeSpan.FromSeconds(15), ct).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                _logger.LogDebug("DashboardService initialization timed out; returning cached values");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DashboardService initialization failed");
            }
        }

        public async Task RefreshAsync(CancellationToken ct = default)
        {
            IEnumerable<BookingDto> items;
            try
            {
                items = await _bookingRepo.GetSnapshotAsync(ct).ConfigureAwait(false) ?? Enumerable.Empty<BookingDto>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch bookings snapshot during Refresh; using previous snapshot");
                items = _snapshot;
            }

            lock (_sync)
            {
                _snapshot = items.ToList();
            }

            ComputeAndUpdateCaches();
        }

        public async Task<decimal> GetTodayRevenueAsync(CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct).ConfigureAwait(false);
            lock (_sync) return _cachedTodayRevenue;
        }

        public async Task<IEnumerable<PaymentMethodStat>> GetPreferredPaymentAsync(StatisticsRange range, CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct).ConfigureAwait(false);
            lock (_sync) return (range == StatisticsRange.Day ? _cachedPaymentsDay : _cachedPaymentsMonth).AsEnumerable();
        }

        public async Task<IEnumerable<TopFoodStat>> GetTopFoodsAsync(StatisticsRange range, int top = 10, CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct).ConfigureAwait(false);
            lock (_sync) return (range == StatisticsRange.Day ? _cachedTopFoodsDay : _cachedTopFoodsMonth).Take(top).AsEnumerable();
        }

        public async Task<IEnumerable<TopUserStat>> GetTopUsersAsync(StatisticsRange range, int top = 10, CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct).ConfigureAwait(false);
            lock (_sync) return (range == StatisticsRange.Day ? _cachedTopUsersDay : _cachedTopUsersMonth).Take(top).AsEnumerable();
        }

        private void ComputeAndUpdateCaches()
        {
            List<BookingDto> snapshot;
            lock (_sync) snapshot = _snapshot.ToList();
            var todayUtc = DateTime.UtcNow.Date;
            var currentYear = DateTime.UtcNow.Year;
            decimal newTodayRevenue = 0m;
            var paymentsDay = new Dictionary<string, (int count, decimal total)>(StringComparer.OrdinalIgnoreCase);
            var paymentsMonth = new Dictionary<string, (int count, decimal total)>(StringComparer.OrdinalIgnoreCase);
            var foodsDay = new Dictionary<string, (int qty, decimal revenue)>(StringComparer.OrdinalIgnoreCase);
            var foodsMonth = new Dictionary<string, (int qty, decimal revenue)>(StringComparer.OrdinalIgnoreCase);
            var usersDay = new Dictionary<string, (decimal total, string displayName)>(StringComparer.OrdinalIgnoreCase);
            var usersMonth = new Dictionary<string, (decimal total, string displayName)>(StringComparer.OrdinalIgnoreCase);

            foreach (var b in snapshot)
            {
                DateTime? createdUtcNullable = TryGetBookingCreatedAt(b);
                if (!createdUtcNullable.HasValue) continue;
                var createdUtc = createdUtcNullable.Value;
                var dayKey = createdUtc.Date;
                decimal bookingTotal = TryGetBookingAmount(b) ?? 0m;
                var method = (!string.IsNullOrWhiteSpace(b.paymentMethod) ? b.paymentMethod : (b.payment != 0 ? b.payment.ToString() : "Unknown")).Trim();
                if (string.IsNullOrEmpty(method)) method = "Unknown";
                var uid = !string.IsNullOrEmpty(b.accountId) ? b.accountId : (!string.IsNullOrEmpty(b.phone) ? b.phone : (!string.IsNullOrEmpty(b.name) ? b.name : "unknown"));
                var displayName = !string.IsNullOrWhiteSpace(b.name) ? b.name : (!string.IsNullOrWhiteSpace(b.phone) ? b.phone : uid);

                if (dayKey == todayUtc)
                {
                    newTodayRevenue += bookingTotal;
                    if (paymentsDay.TryGetValue(method, out var p)) paymentsDay[method] = (p.count + 1, p.total + bookingTotal);
                    else paymentsDay[method] = (1, bookingTotal);
                    if (usersDay.TryGetValue(uid, out var ud)) usersDay[uid] = (ud.total + bookingTotal, string.IsNullOrWhiteSpace(ud.displayName) ? displayName : ud.displayName);
                    else usersDay[uid] = (bookingTotal, displayName);
                }

                if (createdUtc.Year == currentYear)
                {
                    if (paymentsMonth.TryGetValue(method, out var p2)) paymentsMonth[method] = (p2.count + 1, p2.total + bookingTotal);
                    else paymentsMonth[method] = (1, bookingTotal);
                    if (usersMonth.TryGetValue(uid, out var um)) usersMonth[uid] = (um.total + bookingTotal, string.IsNullOrWhiteSpace(um.displayName) ? displayName : um.displayName);
                    else usersMonth[uid] = (bookingTotal, displayName);
                }

                var items = ParseItemsFromFoodsString(b.foods);
                foreach (var it in items)
                {
                    var key = NormalizeKey(it.name);
                    if (string.IsNullOrEmpty(key)) continue;
                    if (dayKey == todayUtc)
                    {
                        foodsDay.TryGetValue(key, out var prev);
                        prev.qty += it.qty;
                        prev.revenue += it.price * it.qty;
                        foodsDay[key] = prev;
                    }
                    if (createdUtc.Year == currentYear)
                    {
                        foodsMonth.TryGetValue(key, out var prev2);
                        prev2.qty += it.qty;
                        prev2.revenue += it.price * it.qty;
                        foodsMonth[key] = prev2;
                    }
                }
            }

            var newPaymentsDay = paymentsDay.Select(kv => new PaymentMethodStat { Method = kv.Key, Count = kv.Value.count, Total = kv.Value.total })
                                           .OrderByDescending(x => x.Total).ToList();
            var newPaymentsMonth = paymentsMonth.Select(kv => new PaymentMethodStat { Method = kv.Key, Count = kv.Value.count, Total = kv.Value.total })
                                               .OrderByDescending(x => x.Total).ToList();

            var newTopFoodsDay = foodsDay.Select(kv => new TopFoodStat { Name = kv.Key, TotalQuantity = kv.Value.qty, TotalRevenue = kv.Value.revenue })
                                         .OrderByDescending(x => x.TotalQuantity).ThenByDescending(x => x.TotalRevenue).ToList();

            var newTopFoodsMonth = foodsMonth.Select(kv => new TopFoodStat { Name = kv.Key, TotalQuantity = kv.Value.qty, TotalRevenue = kv.Value.revenue })
                                             .OrderByDescending(x => x.TotalRevenue).ThenByDescending(x => x.TotalQuantity).ToList();

            var newTopUsersDay = usersDay.Select(kv => new TopUserStat { Id = kv.Key, DisplayName = kv.Value.displayName, TotalSpent = kv.Value.total })
                                         .OrderByDescending(x => x.TotalSpent).ToList();

            var newTopUsersMonth = usersMonth.Select(kv => new TopUserStat { Id = kv.Key, DisplayName = kv.Value.displayName, TotalSpent = kv.Value.total })
                                             .OrderByDescending(x => x.TotalSpent).ToList();

            lock (_sync)
            {
                bool changed = false;
                if (newTodayRevenue != _cachedTodayRevenue)
                {
                    _cachedTodayRevenue = newTodayRevenue;
                    changed = true;
                }

                if (!SequenceEqualPayments(_cachedPaymentsDay, newPaymentsDay))
                {
                    _cachedPaymentsDay = newPaymentsDay;
                    changed = true;
                }

                if (!SequenceEqualPayments(_cachedPaymentsMonth, newPaymentsMonth))
                {
                    _cachedPaymentsMonth = newPaymentsMonth;
                    changed = true;
                }

                if (!SequenceEqualTopFoods(_cachedTopFoodsDay, newTopFoodsDay))
                {
                    _cachedTopFoodsDay = newTopFoodsDay;
                    changed = true;
                }

                if (!SequenceEqualTopFoods(_cachedTopFoodsMonth, newTopFoodsMonth))
                {
                    _cachedTopFoodsMonth = newTopFoodsMonth;
                    changed = true;
                }

                if (!SequenceEqualTopUsers(_cachedTopUsersDay, newTopUsersDay))
                {
                    _cachedTopUsersDay = newTopUsersDay;
                    changed = true;
                }

                if (!SequenceEqualTopUsers(_cachedTopUsersMonth, newTopUsersMonth))
                {
                    _cachedTopUsersMonth = newTopUsersMonth;
                    changed = true;
                }
            }
        }

        private static string NormalizeKey(string? key)
        {
            if (string.IsNullOrWhiteSpace(key)) return string.Empty;
            return Regex.Replace(key.Trim(), @"\s+", " ");
        }

        private static IEnumerable<(string name, decimal price, int qty)> ParseItemsFromFoodsString(string foods)
        {
            var result = new List<(string name, decimal price, int qty)>();
            if (string.IsNullOrWhiteSpace(foods)) return result;
            var lines = foods.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var ln in lines)
            {
                var t = ln.Trim();
                if (string.IsNullOrEmpty(t)) continue;
                var m = _lineItemRegex.Match(t);
                if (m.Success)
                {
                    var name = m.Groups[1].Value.Trim();
                    var priceRaw = m.Groups[2].Value.Trim();
                    var qtyRaw = m.Groups[3].Success ? m.Groups[3].Value.Trim() : "1";
                    var priceStr = priceRaw.Replace(".", "").Replace(",", ".");
                    decimal price = 0m;
                    decimal.TryParse(priceStr, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out price);
                    int qty = 1;
                    int.TryParse(qtyRaw, out qty);
                    result.Add((name, price, qty));
                }
                else
                {
                    var qty = 1;
                    var price = 0m;
                    var mQty = Regex.Match(t, @"Số lượng\s*:?\s*(\d+)", RegexOptions.IgnoreCase);
                    if (mQty.Success) int.TryParse(mQty.Groups[1].Value, out qty);
                    var mPrice = Regex.Match(t, @"([\d\.,]+)\s*VNĐ", RegexOptions.IgnoreCase);
                    if (mPrice.Success)
                    {
                        var p = mPrice.Groups[1].Value.Replace(".", "").Replace(",", ".");
                        decimal.TryParse(p, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out price);
                    }
                    result.Add((t, price, qty));
                }
            }
            return result;
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

        private static bool SequenceEqualPayments(IReadOnlyList<PaymentMethodStat> a, IReadOnlyList<PaymentMethodStat> b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (!string.Equals(a[i].Method, b[i].Method, StringComparison.OrdinalIgnoreCase)) return false;
                if (a[i].Count != b[i].Count) return false;
                if (a[i].Total != b[i].Total) return false;
            }
            return true;
        }

        private static bool SequenceEqualTopFoods(IReadOnlyList<TopFoodStat> a, IReadOnlyList<TopFoodStat> b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (!string.Equals(a[i].Name, b[i].Name, StringComparison.OrdinalIgnoreCase)) return false;
                if (a[i].TotalQuantity != b[i].TotalQuantity) return false;
                if (a[i].TotalRevenue != b[i].TotalRevenue) return false;
            }
            return true;
        }

        private static bool SequenceEqualTopUsers(IReadOnlyList<TopUserStat> a, IReadOnlyList<TopUserStat> b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (!string.Equals(a[i].Id, b[i].Id, StringComparison.OrdinalIgnoreCase)) return false;
                if (a[i].TotalSpent != b[i].TotalSpent) return false;
            }
            return true;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                if (_handler != null)
                {
                    _bookingRepo.RealtimeUpdated -= _handler;
                    _handler = null;
                }
            }
            catch { }
        }
    }
}
