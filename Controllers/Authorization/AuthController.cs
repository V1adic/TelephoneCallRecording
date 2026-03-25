using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;
using TelephoneCallRecording.Services.Authorization.Email;
using TelephoneCallRecording.Services.Authorization.Login;
using TelephoneCallRecording.Services.Authorization.Register;

namespace TelephoneCallRecording.Controllers.Authorization
{
    [ApiController]
    [Route("auth")]
    public class AuthController : Controller
    {
        private readonly ILoginService _loginService;
        private readonly IRegisterService _registerService;
        private readonly IEmailService _emailService;

        public AuthController(
            ILoginService loginService,
            IRegisterService registerService,
            IEmailService emailService)
        {
            _loginService = loginService;
            _registerService = registerService;
            _emailService = emailService;
        }

        public record LoginRequest([Required, MaxLength(15), MinLength(5)] string Username, [Required, MinLength(12), MaxLength(100)] string Password);
        public record RegisterRequest([Required, MaxLength(15), MinLength(5)] string Username, [Required, MinLength(12), MaxLength(100)] string Password, [Required, MaxLength(100)] string Email);
        public record RequestConfirmationRequest([Required, MaxLength(15)] string Username);
        public record ConfirmEmailRequest([Required, MaxLength(15)] string Username, [Required, MaxLength(20)] string Code);

        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var principal = await _loginService.AttemptLogin(request.Username, request.Password);

            if (principal == null)
                return Unauthorized();

            await HttpContext.SignInAsync("cookie", principal);

            return Ok();
        }

        [HttpPost("register")]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var trimmedUsername = request.Username.Trim();
            var trimmedEmail = request.Email.Trim();

            if (await _registerService.AttemptRegister(trimmedUsername, request.Password, trimmedEmail))
            {
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpPost("confirm-email")]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
        {
            if (!await _emailService.AttemptEmailConfirmationAsync(request.Username, request.Code))
            {
                return BadRequest();
            }

            // TODO: Выдать токен для подтвержденного пользователя
            return Ok();
        }

        [Authorize]
        [HttpGet("profile")]
        public IActionResult Profile()
        {
            return Ok(User.Identity!.Name);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("cookie");

            // TODO: Реализовать отзыв токенов при выходе, если будет использоваться JWT
            return Ok();
        }
    }
}