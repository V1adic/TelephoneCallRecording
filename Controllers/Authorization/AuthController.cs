using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TelephoneCallRecording.Services.Authorization.Email;
using TelephoneCallRecording.Services.Authorization.Login;
using TelephoneCallRecording.Services.Authorization.Register;
using TelephoneCallRecording.Services.DataBase.Authorization;
using TelephoneCallRecording.Services.Security;

namespace TelephoneCallRecording.Controllers.Authorization
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        public record AuthMessageResponse(string Code, string Message);
        public record AuthCsrfResponse(string Token);
        public record LoginRequest([Required, MaxLength(15), MinLength(5)] string Username, [Required, MinLength(12), MaxLength(100)] string Password);
        public record RegisterRequest(
            [Required, MaxLength(15), MinLength(5)] string Username,
            [Required, MinLength(12), MaxLength(100)] string Password,
            [Required, EmailAddress, MaxLength(100)] string Email,
            [Required, MaxLength(20), RegularExpression(@"^\+\d{11,15}$")] string PhoneNumber,
            [Required, MaxLength(12), RegularExpression(@"^\d{12}$")] string Inn,
            [Required, MaxLength(250)] string Address,
            [Required, Range(1, int.MaxValue)] int CityId);
        public record ConfirmEmailRequest([Required, MaxLength(20)] string Code);
        public record ProfileResponse(string Username, string Email, string Role, bool IsEmailConfirmed, string? PhoneNumber, string? City);

        private readonly IAntiforgery _antiforgery;
        private readonly ILoginService _loginService;
        private readonly IRegisterService _registerService;
        private readonly IEmailService _emailService;
        private readonly IUserRepository _userRepository;
        private readonly IVerificationCookieService _verificationCookieService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAntiforgery antiforgery,
            ILoginService loginService,
            IRegisterService registerService,
            IEmailService emailService,
            IUserRepository userRepository,
            IVerificationCookieService verificationCookieService,
            ILogger<AuthController> logger)
        {
            _antiforgery = antiforgery;
            _loginService = loginService;
            _registerService = registerService;
            _emailService = emailService;
            _userRepository = userRepository;
            _verificationCookieService = verificationCookieService;
            _logger = logger;
        }

        [HttpGet("csrf")]
        [AllowAnonymous]
        public IActionResult GetCsrfToken()
        {
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            return Ok(new AuthCsrfResponse(tokens.RequestToken ?? string.Empty));
        }

        [HttpPost("login")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _loginService.AttemptLogin(request.Username.Trim(), request.Password);
            if (!result.Success || result.Principal == null)
            {
                _logger.LogWarning(
                    "Login rejected for username {Username} from {RemoteIp}. Code: {Code}.",
                    request.Username.Trim(),
                    HttpContext.Connection.RemoteIpAddress,
                    result.Code);

                return result.Code == "locked"
                    ? StatusCode(StatusCodes.Status423Locked, new AuthMessageResponse(result.Code, result.Message))
                    : Unauthorized(new AuthMessageResponse(result.Code, result.Message));
            }

            await HttpContext.SignInAsync("cookie", result.Principal);

            var rawUserId = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(rawUserId, out var userId))
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new AuthMessageResponse("profile_unavailable", "Не удалось загрузить профиль после входа."));
            }

            var user = await _userRepository.FindByIdAsync(userId);
            if (user == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new AuthMessageResponse("profile_unavailable", "Не удалось загрузить профиль после входа."));
            }

            _logger.LogInformation(
                "User {UserId} ({Username}) signed in successfully from {RemoteIp}.",
                user.Id,
                user.Username,
                HttpContext.Connection.RemoteIpAddress);

            return Ok(CreateProfileResponse(user));
        }

        [HttpPost("register")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await _registerService.AttemptRegister(
                request.Username.Trim(),
                request.Password,
                request.Email.Trim(),
                request.PhoneNumber.Trim(),
                request.Inn.Trim(),
                request.Address.Trim(),
                request.CityId);

            if (result.Success && result.User != null)
            {
                _verificationCookieService.Issue(HttpContext, result.User.Username);
                _logger.LogInformation(
                    "User {UserId} ({Username}) registered successfully from {RemoteIp}.",
                    result.User.Id,
                    result.User.Username,
                    HttpContext.Connection.RemoteIpAddress);
                return Ok(new AuthMessageResponse(result.Code, result.Message));
            }

            _logger.LogWarning(
                "Registration rejected for username {Username} from {RemoteIp}. Code: {Code}.",
                request.Username.Trim(),
                HttpContext.Connection.RemoteIpAddress,
                result.Code);

            return result.Code switch
            {
                "duplicate_username" or "duplicate_email" or "duplicate_phone" => Conflict(new AuthMessageResponse(result.Code, result.Message)),
                "city_not_found" or "invalid_request" => BadRequest(new AuthMessageResponse(result.Code, result.Message)),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new AuthMessageResponse(result.Code, result.Message))
            };
        }

        [HttpPost("request-confirmation")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> RequestConfirmation()
        {
            if (!_verificationCookieService.TryGetUsername(HttpContext, out var username))
            {
                return Unauthorized(new AuthMessageResponse("verification_missing", "Сессия подтверждения истекла."));
            }

            var result = await _emailService.RequestConfirmationCodeAsync(username);
            _logger.LogInformation(
                "Confirmation code request for verification user {Username} from {RemoteIp}. Code: {Code}.",
                username,
                HttpContext.Connection.RemoteIpAddress,
                result.Code);

            return result.Success
                ? Ok(new AuthMessageResponse(result.Code, result.Message))
                : result.Code == "locked"
                    ? StatusCode(StatusCodes.Status423Locked, new AuthMessageResponse(result.Code, result.Message))
                    : BadRequest(new AuthMessageResponse(result.Code, result.Message));
        }

        [HttpPost("confirm-email")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
        {
            if (!_verificationCookieService.TryGetUsername(HttpContext, out var username))
            {
                return Unauthorized(new AuthMessageResponse("verification_missing", "Сессия подтверждения истекла."));
            }

            var result = await _emailService.AttemptEmailConfirmationAsync(username, request.Code.Trim());
            if (!result.Success)
            {
                _logger.LogWarning(
                    "Email confirmation rejected for {Username} from {RemoteIp}. Code: {Code}.",
                    username,
                    HttpContext.Connection.RemoteIpAddress,
                    result.Code);
                return BadRequest(new AuthMessageResponse(result.Code, result.Message));
            }

            _verificationCookieService.Clear(HttpContext);
            _logger.LogInformation(
                "Email confirmed for {Username} from {RemoteIp}.",
                username,
                HttpContext.Connection.RemoteIpAddress);
            return Ok(new AuthMessageResponse(result.Code, result.Message));
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> Profile()
        {
            var userId = TryGetUserId();
            if (userId == null)
            {
                return Unauthorized(new AuthMessageResponse("unauthorized", "Пользователь не авторизован."));
            }

            var user = await _userRepository.FindByIdAsync(userId.Value);
            if (user == null)
            {
                return Unauthorized(new AuthMessageResponse("unauthorized", "Пользователь не найден."));
            }

            return Ok(CreateProfileResponse(user));
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var username = User.FindFirstValue(ClaimTypes.Name) ?? "unknown";
            await HttpContext.SignOutAsync("cookie");

            _logger.LogInformation(
                "User {Username} signed out from {RemoteIp}.",
                username,
                HttpContext.Connection.RemoteIpAddress);

            return Ok(new AuthMessageResponse("logged_out", "Вы вышли из системы."));
        }

        private int? TryGetUserId()
        {
            var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(rawUserId, out var userId) ? userId : null;
        }

        private static ProfileResponse CreateProfileResponse(Models.Authorization.User user)
        {
            return new ProfileResponse(
                user.Username,
                user.Email,
                user.Role,
                user.IsEmailConfirmed,
                user.Subscriber?.PhoneNumber,
                user.Subscriber?.City?.Name);
        }
    }
}
