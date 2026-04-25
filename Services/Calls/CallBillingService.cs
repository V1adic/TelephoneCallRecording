using Microsoft.EntityFrameworkCore;
using TelephoneCallRecording.Models.Calls;
using TelephoneCallRecording.Services.DataBase.Authorization;

namespace TelephoneCallRecording.Services.Calls
{
    public sealed record CityOption(int Id, string Name);
    public sealed record CallHistoryItem(
        int CallId,
        string DestPhone,
        string DestinationCity,
        DateTimeOffset StartedAtUtc,
        int DurationMinutes,
        string TimeOfDay,
        decimal AppliedTariff,
        decimal DiscountPercent,
        decimal BaseCost,
        decimal FinalCost);

    public sealed record CallSummaryView(
        DateTime FromUtc,
        DateTime ToUtc,
        int TotalCalls,
        int TotalMinutes,
        decimal BaseCost,
        decimal TotalCost);

    public sealed record ActiveCallView(string DestPhone, DateTimeOffset StartedAtUtc, string TimeOfDay);

    public sealed record CallStartResult(
        bool Success,
        string Code,
        string Message,
        string? DestPhone = null,
        DateTimeOffset? StartedAtUtc = null,
        string? TimeOfDay = null);

    public sealed record CallEndResult(
        bool Success,
        string Code,
        string Message,
        decimal Cost = 0m,
        decimal DiscountPercent = 0m,
        decimal BaseCost = 0m,
        int DurationMinutes = 0,
        string? DestPhone = null,
        string? TimeOfDay = null);

    public interface ICallBillingService
    {
        Task<IReadOnlyList<CityOption>> GetCitiesAsync(CancellationToken cancellationToken = default);
        Task<ActiveCallView?> GetActiveCallAsync(int userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CallHistoryItem>> GetCallHistoryAsync(int userId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
        Task<CallSummaryView> GetCallSummaryAsync(int userId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
        Task<CallStartResult> StartCallAsync(int userId, string destPhone, CancellationToken cancellationToken = default);
        Task<CallEndResult> EndCallAsync(int userId, string destPhone, CancellationToken cancellationToken = default);
    }

    public class CallBillingService : ICallBillingService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<CallBillingService> _logger;

        public CallBillingService(AppDbContext db, ILogger<CallBillingService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IReadOnlyList<CityOption>> GetCitiesAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Cities
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new CityOption(x.Id, x.Name))
                .ToListAsync(cancellationToken);
        }

        public async Task<ActiveCallView?> GetActiveCallAsync(int userId, CancellationToken cancellationToken = default)
        {
            var subscriberId = await GetSubscriberIdAsync(userId, cancellationToken);
            if (subscriberId == null)
            {
                return null;
            }

            var activeCall = await _db.Calls
                .AsNoTracking()
                .Where(x => x.SubscriberId == subscriberId.Value && x.DurationMinutes == null)
                .OrderByDescending(x => x.StartUnixTime)
                .FirstOrDefaultAsync(cancellationToken);

            if (activeCall == null)
            {
                return null;
            }

            return new ActiveCallView(
                activeCall.DestPhone,
                DateTimeOffset.FromUnixTimeSeconds(activeCall.StartUnixTime),
                ToTransportTimeOfDay(activeCall.TimeOfDay));
        }

        public async Task<IReadOnlyList<CallHistoryItem>> GetCallHistoryAsync(int userId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
        {
            var subscriberId = await GetSubscriberIdAsync(userId, cancellationToken);
            if (subscriberId == null)
            {
                return [];
            }

            var fromUnix = new DateTimeOffset(fromUtc).ToUnixTimeSeconds();
            var toUnix = new DateTimeOffset(toUtc).ToUnixTimeSeconds();

            var history = await _db.Calls
                .AsNoTracking()
                .Where(x => x.SubscriberId == subscriberId.Value)
                .Where(x => x.DurationMinutes.HasValue)
                .Where(x => x.StartUnixTime >= fromUnix && x.StartUnixTime < toUnix)
                .OrderByDescending(x => x.StartUnixTime)
                .Select(x => new
                {
                    x.Id,
                    x.DestPhone,
                    CityName = x.City!.Name,
                    x.StartUnixTime,
                    DurationMinutes = x.DurationMinutes!.Value,
                    x.TimeOfDay,
                    AppliedTariff = x.TimeOfDay == CallTimeOfDay.Day ? x.City!.DayTariff : x.City!.NightTariff,
                    DiscountPercent = _db.CityDiscounts
                        .Where(d => d.CityId == x.CityId &&
                                    x.DurationMinutes.Value >= d.MinMinutes &&
                                    (!d.MaxMinutes.HasValue || x.DurationMinutes.Value < d.MaxMinutes.Value))
                        .OrderByDescending(d => d.MinMinutes)
                        .Select(d => d.DiscountPercent)
                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);

            return history
                .Select(x =>
                {
                    var baseCost = Math.Round(x.DurationMinutes * x.AppliedTariff, 2);
                    var finalCost = Math.Round(baseCost * (1m - x.DiscountPercent / 100m), 2);

                    return new CallHistoryItem(
                        x.Id,
                        x.DestPhone,
                        x.CityName,
                        DateTimeOffset.FromUnixTimeSeconds(x.StartUnixTime),
                        x.DurationMinutes,
                        ToTransportTimeOfDay(x.TimeOfDay),
                        Math.Round(x.AppliedTariff, 2),
                        Math.Round(x.DiscountPercent, 2),
                        baseCost,
                        finalCost);
                })
                .ToList();
        }

        public async Task<CallSummaryView> GetCallSummaryAsync(int userId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
        {
            var history = await GetCallHistoryAsync(userId, fromUtc, toUtc, cancellationToken);

            return new CallSummaryView(
                fromUtc,
                toUtc,
                history.Count,
                history.Sum(x => x.DurationMinutes),
                Math.Round(history.Sum(x => x.BaseCost), 2),
                Math.Round(history.Sum(x => x.FinalCost), 2));
        }

        public async Task<CallStartResult> StartCallAsync(int userId, string destPhone, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(destPhone))
            {
                return new CallStartResult(false, "validation_error", "Номер получателя обязателен.");
            }

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var subscriber = await LoadSubscriberAsync(userId, cancellationToken);
                if (subscriber == null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new CallStartResult(false, "subscriber_not_linked", "За пользователем не закреплён абонент.");
                }

                var destination = await _db.Subscribers
                    .AsNoTracking()
                    .Select(x => new { x.PhoneNumber, x.CityId })
                    .FirstOrDefaultAsync(x => x.PhoneNumber == destPhone, cancellationToken);

                if (destination == null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new CallStartResult(false, "destination_not_found", "Абонент-получатель не найден.");
                }

                var activeExists = await _db.Calls.AnyAsync(
                    x => x.SubscriberId == subscriber.Id &&
                         x.DurationMinutes == null,
                    cancellationToken);

                if (activeExists)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new CallStartResult(false, "active_call_exists", "У вас уже есть активный звонок. Сначала завершите его.");
                }

                var startedAtUtc = DateTimeOffset.UtcNow;
                var serverNow = DateTimeOffset.Now;
                var timeOfDay = serverNow.Hour >= 8 && serverNow.Hour < 20
                    ? CallTimeOfDay.Day
                    : CallTimeOfDay.Night;

                await _db.Calls.AddAsync(new CallRecord
                {
                    SubscriberId = subscriber.Id,
                    CityId = destination.CityId,
                    DestPhone = destPhone,
                    StartUnixTime = startedAtUtc.ToUnixTimeSeconds(),
                    DurationMinutes = null,
                    TimeOfDay = timeOfDay
                }, cancellationToken);

                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "User {UserId} started a call to {DestPhone} at {StartedAtUtc}.",
                    userId,
                    destPhone,
                    startedAtUtc);

                return new CallStartResult(
                    true,
                    "call_started",
                    "Звонок успешно начат.",
                    destPhone,
                    startedAtUtc,
                    ToTransportTimeOfDay(timeOfDay));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Call start conflict for user {UserId} and destination {DestPhone}.", userId, destPhone);
                await transaction.RollbackAsync(cancellationToken);
                return new CallStartResult(false, "active_call_exists", "У вас уже есть активный звонок. Сначала завершите его.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start call for user {UserId}.", userId);
                await transaction.RollbackAsync(cancellationToken);
                return new CallStartResult(false, "server_error", "Не удалось начать звонок.");
            }
        }

        public async Task<CallEndResult> EndCallAsync(int userId, string destPhone, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(destPhone))
            {
                return new CallEndResult(false, "validation_error", "Номер получателя обязателен.");
            }

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var subscriber = await LoadSubscriberAsync(userId, cancellationToken);
                if (subscriber == null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new CallEndResult(false, "subscriber_not_linked", "За пользователем не закреплён абонент.");
                }

                var activeCall = await _db.Calls
                    .FirstOrDefaultAsync(
                        x => x.SubscriberId == subscriber.Id &&
                             x.DestPhone == destPhone &&
                             x.DurationMinutes == null,
                        cancellationToken);

                if (activeCall == null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new CallEndResult(false, "active_call_not_found", "Активный звонок не найден.");
                }

                var city = await _db.Cities.FirstOrDefaultAsync(x => x.Id == activeCall.CityId, cancellationToken);
                if (city == null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new CallEndResult(false, "city_not_found", "Город получателя не найден.");
                }

                var currentUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var secondsElapsed = currentUnix - activeCall.StartUnixTime;
                var durationMinutes = secondsElapsed > 0
                    ? (int)Math.Ceiling(secondsElapsed / 60.0)
                    : 0;

                activeCall.DurationMinutes = durationMinutes;

                var tariff = activeCall.TimeOfDay == CallTimeOfDay.Day
                    ? city.DayTariff
                    : city.NightTariff;

                var discountPercent = await _db.CityDiscounts
                    .Where(x => x.CityId == city.Id &&
                                durationMinutes >= x.MinMinutes &&
                                (!x.MaxMinutes.HasValue || durationMinutes < x.MaxMinutes.Value))
                    .OrderByDescending(x => x.MinMinutes)
                    .Select(x => x.DiscountPercent)
                    .FirstOrDefaultAsync(cancellationToken);

                var baseCost = durationMinutes * tariff;
                var cost = baseCost * (1m - discountPercent / 100m);

                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "User {UserId} completed a call to {DestPhone}. Duration {DurationMinutes} minutes, total {Cost}.",
                    userId,
                    destPhone,
                    durationMinutes,
                    Math.Round(cost, 2));

                return new CallEndResult(
                    true,
                    "call_completed",
                    "Звонок успешно завершён.",
                    Math.Round(cost, 2),
                    Math.Round(discountPercent, 2),
                    Math.Round(baseCost, 2),
                    durationMinutes,
                    destPhone,
                    ToTransportTimeOfDay(activeCall.TimeOfDay));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to end call for user {UserId}.", userId);
                await transaction.RollbackAsync(cancellationToken);
                return new CallEndResult(false, "server_error", "Не удалось завершить звонок.");
            }
        }

        private async Task<Subscriber?> LoadSubscriberAsync(int userId, CancellationToken cancellationToken)
        {
            return await _db.Users
                .Where(x => x.Id == userId)
                .Select(x => x.Subscriber)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<int?> GetSubscriberIdAsync(int userId, CancellationToken cancellationToken)
        {
            return await _db.Users
                .Where(x => x.Id == userId)
                .Select(x => x.SubscriberId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private static string ToTransportTimeOfDay(CallTimeOfDay timeOfDay)
        {
            return timeOfDay == CallTimeOfDay.Day ? "day" : "night";
        }
    }
}
