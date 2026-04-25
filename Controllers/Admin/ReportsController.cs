using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TelephoneCallRecording.Services.DataBase.Authorization;

namespace TelephoneCallRecording.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("reports")]
public class ReportsController : ControllerBase
{
    public record ReportMessageResponse(string Code, string Message);

    private readonly AppDbContext _db;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(AppDbContext db, ILogger<ReportsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet("calls")]
    public async Task<IActionResult> CallDetails([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken cancellationToken)
    {
        if (!TryNormalizePeriod(from, to, out var periodStartUtc, out var periodEndUtc, out var error))
        {
            return BadRequest(new ReportMessageResponse("validation_error", error));
        }

        var result = await _db.Database
            .SqlQueryRaw<CallDetailReport>(
                """
                SELECT
                    c.call_id AS "CallId",
                    s.phone_number AS "SourcePhone",
                    c.dest_phone AS "DestPhone",
                    city.name AS "DestinationCity",
                    to_timestamp(c.start_unix_time) AT TIME ZONE 'UTC' AS "StartedAtUtc",
                    c.duration_minutes AS "DurationMinutes",
                    c.time_of_day AS "TimeOfDay",
                    CASE WHEN c.time_of_day = 'Day' THEN city.day_tariff ELSE city.night_tariff END AS "AppliedTariff",
                    COALESCE(d.discount_percent, 0) AS "DiscountPercent",
                    ROUND(c.duration_minutes * CASE WHEN c.time_of_day = 'Day' THEN city.day_tariff ELSE city.night_tariff END, 2) AS "BaseCost",
                    ROUND(c.duration_minutes * CASE WHEN c.time_of_day = 'Day' THEN city.day_tariff ELSE city.night_tariff END * (1 - COALESCE(d.discount_percent, 0) / 100.0), 2) AS "FinalCost"
                FROM calls c
                JOIN subscribers s ON s.subscriber_id = c.subscriber_id
                JOIN cities city ON city.city_id = c.city_id
                LEFT JOIN LATERAL (
                    SELECT discount_percent
                    FROM city_discounts
                    WHERE city_id = c.city_id
                      AND c.duration_minutes >= min_minutes
                      AND (max_minutes IS NULL OR c.duration_minutes < max_minutes)
                    ORDER BY min_minutes DESC LIMIT 1
                ) d ON true
                WHERE c.duration_minutes IS NOT NULL
                  AND to_timestamp(c.start_unix_time) >= @from
                  AND to_timestamp(c.start_unix_time) < @to
                ORDER BY c.start_unix_time DESC
                """,
                new Npgsql.NpgsqlParameter("@from", periodStartUtc),
                new Npgsql.NpgsqlParameter("@to", periodEndUtc))
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Admin {AdminName} generated detailed calls report for period {FromUtc} - {ToUtc}.",
            User.Identity?.Name,
            periodStartUtc,
            periodEndUtc);

        return Ok(result);
    }

    [HttpGet("cities")]
    public async Task<IActionResult> CityCosts([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken cancellationToken)
    {
        if (!TryNormalizePeriod(from, to, out var periodStartUtc, out var periodEndUtc, out var error))
        {
            return BadRequest(new ReportMessageResponse("validation_error", error));
        }

        var result = await _db.Database
            .SqlQueryRaw<CityCostReport>(
                """
                SELECT
                    city.name AS "Name",
                    COUNT(*)::bigint AS "TotalCalls",
                    COALESCE(SUM(c.duration_minutes), 0)::bigint AS "TotalMinutes",
                    COALESCE(ROUND(SUM(c.duration_minutes * CASE WHEN c.time_of_day = 'Day' THEN city.day_tariff ELSE city.night_tariff END * (1 - COALESCE(d.discount_percent, 0) / 100.0)), 2), 0) AS "TotalCost"
                FROM calls c
                JOIN cities city ON city.city_id = c.city_id
                LEFT JOIN LATERAL (
                    SELECT discount_percent
                    FROM city_discounts
                    WHERE city_id = c.city_id
                      AND c.duration_minutes >= min_minutes
                      AND (max_minutes IS NULL OR c.duration_minutes < max_minutes)
                    ORDER BY min_minutes DESC LIMIT 1
                ) d ON true
                WHERE c.duration_minutes IS NOT NULL
                  AND to_timestamp(c.start_unix_time) >= @from
                  AND to_timestamp(c.start_unix_time) < @to
                GROUP BY city.name
                ORDER BY "TotalCost" DESC, city.name
                """,
                new Npgsql.NpgsqlParameter("@from", periodStartUtc),
                new Npgsql.NpgsqlParameter("@to", periodEndUtc))
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Admin {AdminName} generated city costs report for period {FromUtc} - {ToUtc}.",
            User.Identity?.Name,
            periodStartUtc,
            periodEndUtc);

        return Ok(result);
    }

    [HttpGet("subscriber/{phoneNumber}")]
    public async Task<IActionResult> SubscriberCosts(string phoneNumber, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken cancellationToken)
    {
        if (!Regex.IsMatch(phoneNumber, @"^\+\d{11,15}$"))
        {
            return BadRequest(new ReportMessageResponse("validation_error", "Номер абонента указан в неверном формате."));
        }

        if (!TryNormalizePeriod(from, to, out var periodStartUtc, out var periodEndUtc, out var error))
        {
            return BadRequest(new ReportMessageResponse("validation_error", error));
        }

        var result = await _db.Database
            .SqlQueryRaw<SubscriberCostReport>(
                """
                SELECT
                    s.phone_number AS "PhoneNumber",
                    COUNT(*)::bigint AS "TotalCalls",
                    COALESCE(SUM(c.duration_minutes), 0)::bigint AS "TotalMinutes",
                    COALESCE(ROUND(SUM(c.duration_minutes * CASE WHEN c.time_of_day = 'Day' THEN city.day_tariff ELSE city.night_tariff END * (1 - COALESCE(d.discount_percent, 0) / 100.0)), 2), 0) AS "TotalCost"
                FROM calls c
                JOIN subscribers s ON s.subscriber_id = c.subscriber_id
                JOIN cities city ON city.city_id = c.city_id
                LEFT JOIN LATERAL (
                    SELECT discount_percent
                    FROM city_discounts
                    WHERE city_id = c.city_id
                      AND c.duration_minutes >= min_minutes
                      AND (max_minutes IS NULL OR c.duration_minutes < max_minutes)
                    ORDER BY min_minutes DESC LIMIT 1
                ) d ON true
                WHERE c.duration_minutes IS NOT NULL
                  AND s.phone_number = @phone
                  AND to_timestamp(c.start_unix_time) >= @from
                  AND to_timestamp(c.start_unix_time) < @to
                GROUP BY s.phone_number
                """,
                new Npgsql.NpgsqlParameter("@phone", phoneNumber),
                new Npgsql.NpgsqlParameter("@from", periodStartUtc),
                new Npgsql.NpgsqlParameter("@to", periodEndUtc))
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Admin {AdminName} generated subscriber report for {PhoneNumber} and period {FromUtc} - {ToUtc}.",
            User.Identity?.Name,
            phoneNumber,
            periodStartUtc,
            periodEndUtc);

        return Ok(result);
    }

    private static bool TryNormalizePeriod(DateTime from, DateTime to, out DateTime periodStartUtc, out DateTime periodEndUtc, out string error)
    {
        periodStartUtc = from.ToUniversalTime();
        periodEndUtc = to.ToUniversalTime();
        error = string.Empty;

        if (periodStartUtc >= periodEndUtc)
        {
            error = "Дата начала периода должна быть раньше даты окончания.";
            return false;
        }

        if ((periodEndUtc - periodStartUtc).TotalDays > 366)
        {
            error = "Период отчёта не должен превышать 366 дней.";
            return false;
        }

        return true;
    }
}

public record CallDetailReport(
    int CallId,
    string SourcePhone,
    string DestPhone,
    string DestinationCity,
    DateTime StartedAtUtc,
    int DurationMinutes,
    string TimeOfDay,
    decimal AppliedTariff,
    decimal DiscountPercent,
    decimal BaseCost,
    decimal FinalCost);

public record CityCostReport(string Name, long TotalCalls, long TotalMinutes, decimal TotalCost);
public record SubscriberCostReport(string PhoneNumber, long TotalCalls, long TotalMinutes, decimal TotalCost);
