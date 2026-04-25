using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TelephoneCallRecording.Models.Calls;
using TelephoneCallRecording.Services.DataBase.Authorization;

namespace TelephoneCallRecording.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("admin")]
public class ManagementController : ControllerBase
{
    public record AdminMessageResponse(string Code, string Message);
    public record CityResponse(int Id, string Name, decimal DayTariff, decimal NightTariff, int SubscribersCount, int CallsCount);
    public record DiscountResponse(int Id, int CityId, int MinMinutes, int? MaxMinutes, decimal DiscountPercent);
    public record AdminUserResponse(
        int Id,
        string Username,
        string Email,
        string Role,
        bool IsEmailConfirmed,
        int? SubscriberId,
        string? Phone,
        string? Inn,
        string? Address,
        int? CityId,
        string? CityName,
        DateTime? LockoutEndUtc,
        string AccessState);

    public record CreateCityRequest(
        [Required, StringLength(100, MinimumLength = 2)] string Name,
        [Range(typeof(decimal), "0.01", "1000000")] decimal DayTariff,
        [Range(typeof(decimal), "0.01", "1000000")] decimal NightTariff);

    public record UpdateCityRequest(
        [Required, StringLength(100, MinimumLength = 2)] string Name,
        [Range(typeof(decimal), "0.01", "1000000")] decimal DayTariff,
        [Range(typeof(decimal), "0.01", "1000000")] decimal NightTariff);

    public record CreateDiscountRequest(
        [Range(1, int.MaxValue)] int CityId,
        [Range(1, 525600)] int MinMinutes,
        int? MaxMinutes,
        [Range(typeof(decimal), "0", "100")] decimal DiscountPercent);

    public record ChangeRoleRequest([Required] string Role);

    public record UpdateSubscriberRequest(
        [Required, MaxLength(20), RegularExpression(@"^\+\d{11,15}$")] string PhoneNumber,
        [Required, MaxLength(12), RegularExpression(@"^\d{12}$")] string Inn,
        [Required, MaxLength(250), MinLength(5)] string Address,
        [Range(1, int.MaxValue)] int CityId);

    private readonly AppDbContext _db;
    private readonly ILogger<ManagementController> _logger;

    public ManagementController(AppDbContext db, ILogger<ManagementController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet("cities")]
    public async Task<IActionResult> GetCities(CancellationToken cancellationToken)
    {
        var cities = await _db.Cities
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(city => new CityResponse(
                city.Id,
                city.Name,
                city.DayTariff,
                city.NightTariff,
                city.Subscribers.Count,
                city.Calls.Count))
            .ToListAsync(cancellationToken);

        return Ok(cities);
    }

    [HttpPost("cities")]
    public async Task<IActionResult> CreateCity([FromBody] CreateCityRequest request, CancellationToken cancellationToken)
    {
        var normalizedName = request.Name.Trim();

        if (await _db.Cities.AnyAsync(x => x.Name == normalizedName, cancellationToken))
        {
            return Conflict(new AdminMessageResponse("city_exists", "Город уже существует."));
        }

        var city = new City
        {
            Name = normalizedName,
            DayTariff = Math.Round(request.DayTariff, 2),
            NightTariff = Math.Round(request.NightTariff, 2)
        };

        _db.Cities.Add(city);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Admin {ActorUserId} created city {CityId} ({CityName}).",
            TryGetActorUserId(),
            city.Id,
            city.Name);

        return Ok(new CityResponse(city.Id, city.Name, city.DayTariff, city.NightTariff, 0, 0));
    }

    [HttpPut("cities/{id:int}")]
    public async Task<IActionResult> UpdateCity(int id, [FromBody] UpdateCityRequest request, CancellationToken cancellationToken)
    {
        var city = await _db.Cities.FindAsync([id], cancellationToken);
        if (city == null)
        {
            return NotFound(new AdminMessageResponse("city_not_found", "Город не найден."));
        }

        var normalizedName = request.Name.Trim();
        var duplicateExists = await _db.Cities.AnyAsync(x => x.Id != id && x.Name == normalizedName, cancellationToken);
        if (duplicateExists)
        {
            return Conflict(new AdminMessageResponse("city_exists", "Город с таким названием уже существует."));
        }

        city.Name = normalizedName;
        city.DayTariff = Math.Round(request.DayTariff, 2);
        city.NightTariff = Math.Round(request.NightTariff, 2);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Admin {ActorUserId} updated city {CityId}.",
            TryGetActorUserId(),
            city.Id);

        var subscribersCount = await _db.Subscribers.CountAsync(x => x.CityId == city.Id, cancellationToken);
        var callsCount = await _db.Calls.CountAsync(x => x.CityId == city.Id, cancellationToken);

        return Ok(new CityResponse(city.Id, city.Name, city.DayTariff, city.NightTariff, subscribersCount, callsCount));
    }

    [HttpDelete("cities/{id:int}")]
    public async Task<IActionResult> DeleteCity(int id, CancellationToken cancellationToken)
    {
        var city = await _db.Cities.FindAsync([id], cancellationToken);
        if (city == null)
        {
            return NotFound(new AdminMessageResponse("city_not_found", "Город не найден."));
        }

        var isUsedBySubscribers = await _db.Subscribers.AnyAsync(x => x.CityId == id, cancellationToken);
        var isUsedByCalls = await _db.Calls.AnyAsync(x => x.CityId == id, cancellationToken);
        if (isUsedBySubscribers || isUsedByCalls)
        {
            return Conflict(new AdminMessageResponse("city_in_use", "Нельзя удалить город, который используется в абонентах или истории звонков."));
        }

        _db.Cities.Remove(city);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Admin {ActorUserId} deleted city {CityId} ({CityName}).",
            TryGetActorUserId(),
            city.Id,
            city.Name);

        return NoContent();
    }

    [HttpPost("discounts")]
    public async Task<IActionResult> CreateDiscount([FromBody] CreateDiscountRequest request, CancellationToken cancellationToken)
    {
        if (request.MaxMinutes.HasValue && request.MaxMinutes.Value <= request.MinMinutes)
        {
            return BadRequest(new AdminMessageResponse("discount_range_invalid", "Верхняя граница диапазона должна быть больше нижней."));
        }

        var cityExists = await _db.Cities.AnyAsync(x => x.Id == request.CityId, cancellationToken);
        if (!cityExists)
        {
            return NotFound(new AdminMessageResponse("city_not_found", "Город не найден."));
        }

        var existingDiscounts = await _db.CityDiscounts
            .AsNoTracking()
            .Where(x => x.CityId == request.CityId)
            .ToListAsync(cancellationToken);

        if (existingDiscounts.Any(x => RangesOverlap(x.MinMinutes, x.MaxMinutes, request.MinMinutes, request.MaxMinutes)))
        {
            return Conflict(new AdminMessageResponse("discount_overlap", "Для этого диапазона минут уже настроено правило скидки."));
        }

        var discount = new CityDiscount
        {
            CityId = request.CityId,
            MinMinutes = request.MinMinutes,
            MaxMinutes = request.MaxMinutes,
            DiscountPercent = Math.Round(request.DiscountPercent, 2)
        };

        _db.CityDiscounts.Add(discount);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Admin {ActorUserId} created discount {DiscountId} for city {CityId}.",
            TryGetActorUserId(),
            discount.Id,
            discount.CityId);

        return Ok(new DiscountResponse(discount.Id, discount.CityId, discount.MinMinutes, discount.MaxMinutes, discount.DiscountPercent));
    }

    [HttpGet("discounts/{cityId:int}")]
    public async Task<IActionResult> GetDiscounts(int cityId, CancellationToken cancellationToken)
    {
        var discounts = await _db.CityDiscounts
            .AsNoTracking()
            .Where(x => x.CityId == cityId)
            .OrderBy(x => x.MinMinutes)
            .Select(x => new DiscountResponse(x.Id, x.CityId, x.MinMinutes, x.MaxMinutes, x.DiscountPercent))
            .ToListAsync(cancellationToken);

        return Ok(discounts);
    }

    [HttpDelete("discounts/{id:int}")]
    public async Task<IActionResult> DeleteDiscount(int id, CancellationToken cancellationToken)
    {
        var discount = await _db.CityDiscounts.FindAsync([id], cancellationToken);
        if (discount == null)
        {
            return NotFound(new AdminMessageResponse("discount_not_found", "Скидка не найдена."));
        }

        _db.CityDiscounts.Remove(discount);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Admin {ActorUserId} deleted discount {DiscountId}.",
            TryGetActorUserId(),
            discount.Id);

        return NoContent();
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var revokedBoundary = now.AddYears(10);

        var users = await _db.Users
            .AsNoTracking()
            .Include(x => x.Subscriber)
            .ThenInclude(x => x!.City)
            .OrderBy(x => x.Username)
            .Select(user => new AdminUserResponse(
                user.Id,
                user.Username,
                user.Email,
                user.Role,
                user.IsEmailConfirmed,
                user.SubscriberId,
                user.Subscriber != null ? user.Subscriber.PhoneNumber : null,
                user.Subscriber != null ? user.Subscriber.Inn : null,
                user.Subscriber != null ? user.Subscriber.Address : null,
                user.Subscriber != null ? user.Subscriber.CityId : null,
                user.Subscriber != null && user.Subscriber.City != null ? user.Subscriber.City.Name : null,
                user.LockoutEnd,
                user.LockoutEnd.HasValue && user.LockoutEnd.Value > revokedBoundary
                    ? "revoked"
                    : user.LockoutEnd.HasValue && user.LockoutEnd.Value > now
                        ? "locked"
                        : "active"))
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpPatch("users/{id:int}/role")]
    public async Task<IActionResult> ChangeRole(int id, [FromBody] ChangeRoleRequest request, CancellationToken cancellationToken)
    {
        var normalizedRole = request.Role.Trim();
        if (normalizedRole is not ("Admin" or "Client"))
        {
            return BadRequest(new AdminMessageResponse("invalid_role", "Роль может быть только Admin или Client."));
        }

        var user = await _db.Users.FindAsync([id], cancellationToken);
        if (user == null)
        {
            return NotFound(new AdminMessageResponse("user_not_found", "Пользователь не найден."));
        }

        if (user.Role == "Admin" && normalizedRole != "Admin")
        {
            var adminsCount = await _db.Users.CountAsync(x => x.Role == "Admin", cancellationToken);
            if (adminsCount <= 1)
            {
                return BadRequest(new AdminMessageResponse("last_admin", "Нельзя изменить роль последнего администратора."));
            }
        }

        user.Role = normalizedRole;
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Admin {ActorUserId} changed role for user {TargetUserId} to {Role}.",
            TryGetActorUserId(),
            user.Id,
            user.Role);

        return Ok(new AdminMessageResponse("role_updated", $"Роль изменена на {normalizedRole}."));
    }

    [HttpPut("users/{id:int}/subscriber")]
    public async Task<IActionResult> UpdateSubscriber(int id, [FromBody] UpdateSubscriberRequest request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Include(x => x.Subscriber)
            .ThenInclude(x => x!.City)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (user?.Subscriber == null)
        {
            return NotFound(new AdminMessageResponse("subscriber_not_found", "Профиль абонента не найден."));
        }

        var cityExists = await _db.Cities.AnyAsync(x => x.Id == request.CityId, cancellationToken);
        if (!cityExists)
        {
            return NotFound(new AdminMessageResponse("city_not_found", "Город не найден."));
        }

        var normalizedPhone = request.PhoneNumber.Trim();
        var phoneBusy = await _db.Subscribers.AnyAsync(
            x => x.Id != user.Subscriber.Id && x.PhoneNumber == normalizedPhone,
            cancellationToken);

        if (phoneBusy)
        {
            return Conflict(new AdminMessageResponse("duplicate_phone", "Абонент с таким номером уже существует."));
        }

        user.Subscriber.PhoneNumber = normalizedPhone;
        user.Subscriber.Inn = request.Inn.Trim();
        user.Subscriber.Address = request.Address.Trim();
        user.Subscriber.CityId = request.CityId;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Admin {ActorUserId} updated subscriber profile for user {TargetUserId}.",
            TryGetActorUserId(),
            user.Id);

        var cityName = await _db.Cities
            .Where(x => x.Id == request.CityId)
            .Select(x => x.Name)
            .FirstAsync(cancellationToken);

        var accessState = ResolveAccessState(user.LockoutEnd, DateTime.UtcNow);

        return Ok(new AdminUserResponse(
            user.Id,
            user.Username,
            user.Email,
            user.Role,
            user.IsEmailConfirmed,
            user.SubscriberId,
            user.Subscriber.PhoneNumber,
            user.Subscriber.Inn,
            user.Subscriber.Address,
            user.Subscriber.CityId,
            cityName,
            user.LockoutEnd,
            accessState));
    }

    [HttpPost("users/{id:int}/revoke")]
    public async Task<IActionResult> RevokeUserAccess(int id, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FindAsync([id], cancellationToken);
        if (user == null)
        {
            return NotFound(new AdminMessageResponse("user_not_found", "Пользователь не найден."));
        }

        if (user.Role == "Admin")
        {
            return BadRequest(new AdminMessageResponse("cannot_revoke_admin", "Доступ администратора отзывать нельзя."));
        }

        user.LockoutEnd = DateTime.UtcNow.AddYears(50);
        user.FailedLoginAttempts = 0;
        user.FailedEmailConfirmAttempts = 0;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Admin {ActorUserId} revoked access for user {TargetUserId}.",
            TryGetActorUserId(),
            user.Id);

        return Ok(new AdminMessageResponse("access_revoked", "Доступ пользователя отозван."));
    }

    [HttpPost("users/{id:int}/restore")]
    public async Task<IActionResult> RestoreUserAccess(int id, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FindAsync([id], cancellationToken);
        if (user == null)
        {
            return NotFound(new AdminMessageResponse("user_not_found", "Пользователь не найден."));
        }

        user.LockoutEnd = null;
        user.FailedLoginAttempts = 0;
        user.FailedEmailConfirmAttempts = 0;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Admin {ActorUserId} restored access for user {TargetUserId}.",
            TryGetActorUserId(),
            user.Id);

        return Ok(new AdminMessageResponse("access_restored", "Доступ пользователя восстановлен."));
    }

    [HttpDelete("users/{id:int}")]
    public Task<IActionResult> RevokeUser(int id, CancellationToken cancellationToken)
    {
        return RevokeUserAccess(id, cancellationToken);
    }

    private int? TryGetActorUserId()
    {
        var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(rawUserId, out var userId) ? userId : null;
    }

    private static bool RangesOverlap(int minA, int? maxA, int minB, int? maxB)
    {
        var normalizedMaxA = maxA ?? int.MaxValue;
        var normalizedMaxB = maxB ?? int.MaxValue;
        return minA < normalizedMaxB && minB < normalizedMaxA;
    }

    private static string ResolveAccessState(DateTime? lockoutEndUtc, DateTime nowUtc)
    {
        if (!lockoutEndUtc.HasValue || lockoutEndUtc <= nowUtc)
        {
            return "active";
        }

        return lockoutEndUtc.Value > nowUtc.AddYears(10) ? "revoked" : "locked";
    }
}
