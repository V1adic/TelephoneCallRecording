using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
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
        public record StartCallRequest([Required, MaxLength(20)] string DestPhone);
        public record EndCallRequest([Required, MaxLength(20)] string DestPhone);

        private readonly IAntiforgery _antiforgery;
        private readonly ICallBillingService _callBillingService;

        public CallsController(IAntiforgery antiforgery, ICallBillingService callBillingService)
        {
            _antiforgery = antiforgery;
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
            if (!await ValidateCsrfAsync())
            {
                return BadRequest(new CallsMessageResponse("csrf_invalid", "Токен CSRF недействителен."));
            }

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
            if (!await ValidateCsrfAsync())
            {
                return BadRequest(new CallsMessageResponse("csrf_invalid", "Токен CSRF недействителен."));
            }

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

        private int? TryGetUserId()
        {
            var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(rawUserId, out var userId) ? userId : null;
        }

        private async Task<bool> ValidateCsrfAsync()
        {
            try
            {
                await _antiforgery.ValidateRequestAsync(HttpContext);
                return true;
            }
            catch (AntiforgeryValidationException)
            {
                return false;
            }
        }
    }
}
