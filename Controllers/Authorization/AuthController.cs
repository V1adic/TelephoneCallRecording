using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using TelephoneCallRecording.Models.Authorization;
using TelephoneCallRecording.Services.Authorization;
using TelephoneCallRecording.Services.Authorization.Login;

namespace TelephoneCallRecording.Controllers.Authorization
{
    [ApiController]
    [Route("auth")]
    public class AuthController(AppDbContext db) : Controller
    {
        private readonly AppDbContext _db = db;
        private const string PasswordRegex = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&^#()_+\-=\[\]{}|;:,.<>/?])[A-Za-z\d@$!%*?&^#()_+\-=\[\]{}|;:,.<>/?]{12,}$";

        public record LoginRequest([Required, MaxLength(15), MinLength(5)] string Username, [Required, MinLength(12), MaxLength(100)] string Password);
        public record RegisterRequest([Required, MaxLength(15), MinLength(5)] string Username, [Required, MinLength(12), MaxLength(100)] string Password, [Required, MaxLength(100)] string Email);
        public record ConfirmEmailRequest([Required, MaxLength(100)] string Email, [Required, MaxLength(100)] string Code);

        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var principal = await LoginService.AttemptLogin(_db, request.Username, request.Password);

            if (principal == null) // Неудачная попытка входа, например, из-за неверного пароля или заблокированного аккаунта
                return Unauthorized();

            await HttpContext.SignInAsync("cookie", principal);

            return Ok();
        }


        [HttpPost("register")]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!Regex.IsMatch(request.Password, PasswordRegex))
                return BadRequest("Password error");

            var trimmedUsername = request.Username.Trim();

            if (trimmedUsername.Length > 15 ||
                trimmedUsername.Length < 5 ||
                string.IsNullOrEmpty(trimmedUsername) ||
                trimmedUsername.Contains(' '))
                return BadRequest("Username error");

            if (!EmailService.IsValidEmail(request.Email.Trim()))
                return BadRequest("Email error");

            var (hash, salt) = PasswordHasher.HashPassword(request.Password);

            var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            var bytes = Encoding.UTF8.GetBytes(code);
            string codeHash = Convert.ToBase64String(SHA256.HashData(bytes));

            if (await _db.Users.AnyAsync(x => x.Username == trimmedUsername))
                return BadRequest();

            var user = new User
            {
                Email = request.Email.Trim(),
                Username = trimmedUsername,
                PasswordHash = hash,
                PasswordSalt = salt,
                IsEmailConfirmed = false,
                EmailConfirmationCodeHash = codeHash,
                EmailConfirmationExpires = DateTime.UtcNow.AddMinutes(10)
            };

            _db.Users.Add(user);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return BadRequest();
            }

            await EmailService.SendConfirmationCodeAsync(request.Email.Trim(), code);

            return Ok();
        }

        [HttpPost("confirm-email")]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
        {
            var bytes = Encoding.UTF8.GetBytes(request.Code);
            string incomingCodeHash = Convert.ToBase64String(SHA256.HashData(bytes));

            var user = await _db.Users
                .FirstOrDefaultAsync(x => x.Email == request.Email.Trim());

            if (user == null)
            {
                return Unauthorized();
            }
            else if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            {
                return BadRequest("Account locked");
            }
            else if (user.IsEmailConfirmed)
            {
                user.FailedEmailConfirmAttempts++;
                if (user.FailedEmailConfirmAttempts >= 5)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                }
                await _db.SaveChangesAsync();

                return BadRequest("Already confirmed");
            }
            else if (user.EmailConfirmationCodeHash != incomingCodeHash)
            {
                user.FailedEmailConfirmAttempts++;
                if (user.FailedEmailConfirmAttempts >= 5)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                }
                await _db.SaveChangesAsync();

                return BadRequest("Invalid code");
            }
            else if (user.EmailConfirmationExpires < DateTime.UtcNow)
            {
                user.FailedEmailConfirmAttempts++;
                if (user.FailedEmailConfirmAttempts >= 5)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                }
                await _db.SaveChangesAsync();

                return BadRequest("Code expired");
            }

            // Сброс счетчика неудачных попыток подтверждения email при успешном подтверждении
            user.IsEmailConfirmed = true;
            user.EmailConfirmationCodeHash = null;
            user.EmailConfirmationExpires = null;
            user.FailedEmailConfirmAttempts = 0;
            user.LockoutEnd = null;

            await _db.SaveChangesAsync();

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