using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TelephoneCallRecording.Services.Calls;

namespace TelephoneCallRecording.Controllers
{
    [ApiController]
    [Route("calls")]
    public class CallsController : ControllerBase
    {
        public record CallsMessageResponse(string Code, string Message);
        public record StartCallRequest([Required, MaxLength(20), RegularExpression(@"^\+\d{11,15}$")] string DestPhone);
        public record EndCallRequest([Required, MaxLength(20), RegularExpression(@"^\+\d{11,15}$")] string DestPhone);
        private readonly ICallBillingService _callBillingService;

        public CallsController(ICallBillingService callBillingService)
        {
            _callBillingService = callBillingService;
        }

        [AllowAnonymous]
        [HttpGet("cities")]
        public async Task<IActionResult> GetCities(CancellationToken cancellationToken)
        {
            var cities = await _callBillingService.GetCitiesAsync(cancellationToken);
            return Ok(cities);
        }

        [Authorize]
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveCall(CancellationToken cancellationToken)
        {
            var userId = TryGetUserId();
            if (userId == null)
            {
                return Unauthorized(new CallsMessageResponse("unauthorized", "Требуется авторизация."));
            }

            var activeCall = await _callBillingService.GetActiveCallAsync(userId.Value, cancellationToken);
            return Ok(new { activeCall });
        }

        [Authorize]
        [EnableRateLimiting("calls")]
        [HttpPost("start")]
        public async Task<IActionResult> StartCall([FromBody] StartCallRequest request, CancellationToken cancellationToken)
        {
            var userId = TryGetUserId();
            if (userId == null)
            {
                return Unauthorized(new CallsMessageResponse("unauthorized", "Требуется авторизация."));
            }

            var result = await _callBillingService.StartCallAsync(userId.Value, request.DestPhone.Trim(), cancellationToken);
            return result.Code switch
            {
                "call_started" => Ok(result),
                "active_call_exists" => Conflict(new CallsMessageResponse(result.Code, result.Message)),
                "destination_not_found" => NotFound(new CallsMessageResponse(result.Code, result.Message)),
                "subscriber_not_linked" or "validation_error" => BadRequest(new CallsMessageResponse(result.Code, result.Message)),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new CallsMessageResponse(result.Code, result.Message))
            };
        }

        [Authorize]
        [EnableRateLimiting("calls")]
        [HttpPost("end")]
        public async Task<IActionResult> EndCall([FromBody] EndCallRequest request, CancellationToken cancellationToken)
        {
            var userId = TryGetUserId();
            if (userId == null)
            {
                return Unauthorized(new CallsMessageResponse("unauthorized", "Требуется авторизация."));
            }

            var result = await _callBillingService.EndCallAsync(userId.Value, request.DestPhone.Trim(), cancellationToken);
            return result.Code switch
            {
                "call_completed" => Ok(result),
                "active_call_not_found" => NotFound(new CallsMessageResponse(result.Code, result.Message)),
                "subscriber_not_linked" or "validation_error" => BadRequest(new CallsMessageResponse(result.Code, result.Message)),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new CallsMessageResponse(result.Code, result.Message))
            };
        }

        [Authorize]
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken)
        {
            var userId = TryGetUserId();
            if (userId == null)
            {
                return Unauthorized(new CallsMessageResponse("unauthorized", "Требуется авторизация."));
            }

            if (!TryNormalizePeriod(from, to, out var periodStartUtc, out var periodEndUtc, out var error))
            {
                return BadRequest(new CallsMessageResponse("validation_error", error));
            }

            var history = await _callBillingService.GetCallHistoryAsync(userId.Value, periodStartUtc, periodEndUtc, cancellationToken);
            return Ok(history);
        }

        [Authorize]
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken)
        {
            var userId = TryGetUserId();
            if (userId == null)
            {
                return Unauthorized(new CallsMessageResponse("unauthorized", "Требуется авторизация."));
            }

            if (!TryNormalizePeriod(from, to, out var periodStartUtc, out var periodEndUtc, out var error))
            {
                return BadRequest(new CallsMessageResponse("validation_error", error));
            }

            var summary = await _callBillingService.GetCallSummaryAsync(userId.Value, periodStartUtc, periodEndUtc, cancellationToken);
            return Ok(summary);
        }

        private int? TryGetUserId()
        {
            var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(rawUserId, out var userId) ? userId : null;
        }

        private static bool TryNormalizePeriod(DateTime? from, DateTime? to, out DateTime periodStartUtc, out DateTime periodEndUtc, out string error)
        {
            periodEndUtc = (to ?? DateTime.UtcNow).ToUniversalTime();
            periodStartUtc = (from ?? periodEndUtc.AddDays(-30)).ToUniversalTime();
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
}
