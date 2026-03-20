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

namespace TelephoneCallRecording.Controllers.Authorization
{
    [ApiController]
    [Route("auth")]
    public class AuthController : Controller
    {
        private readonly AppDbContext _db;
        private static readonly string DummySalt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        private static readonly string DummyHash = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        private const string PasswordRegex = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&^#()_+\-=\[\]{}|;:,.<>/?])[A-Za-z\d@$!%*?&^#()_+\-=\[\]{}|;:,.<>/?]{12,}$";

        public record LoginRequest([Required, MaxLength(15), MinLength(5)] string Username, [Required, MinLength(12), MaxLength(100)] string Password);
        public record RegisterRequest([Required, MaxLength(15), MinLength(5)] string Username, [Required, MinLength(12), MaxLength(100)] string Password, [Required, MaxLength(100)] string Email);
        public record ConfirmEmailRequest([Required, MaxLength(100)] string Email, [Required, MaxLength(100)] string Code);

        public AuthController(AppDbContext db)
        {
            _db = db;
        }


        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();

            var user = await _db.Users.FirstOrDefaultAsync(x => x.Username == request.Username.Trim());

            bool accountLocked = false;

            if (user != null)
            {
                if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
                {
                    accountLocked = true;
                }
                else if (user.LockoutEnd.HasValue)
                {
                    user.FailedLoginAttempts = 0;
                    user.LockoutEnd = null;
                }
            }

            string hashToUse = (user == null || accountLocked) ? DummyHash : user.PasswordHash;
            string saltToUse = (user == null || accountLocked) ? DummySalt : user.PasswordSalt;

            bool passwordCorrect = PasswordHasher.Verify(request.Password, hashToUse, saltToUse);

            if (user == null || accountLocked)
                return Unauthorized();

            if (!passwordCorrect)
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                }
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return Unauthorized();
            }

            // Сброс ошибок
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            var claims = new List<Claim>
            {
                new (ClaimTypes.Name, user.Username),
                new (ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            var identity = new ClaimsIdentity(claims, "cookie");

            await HttpContext.SignInAsync("cookie",
                new ClaimsPrincipal(identity));

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

            if(!EmailService.IsValidEmail(request.Email.Trim()))
                return BadRequest("Email error");

            var (hash, salt) = PasswordHasher.HashPassword(request.Password);

            var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(code);
            string codeHash = Convert.ToBase64String(sha.ComputeHash(bytes));

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
            catch(DbUpdateException)
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
            await using var tx = await _db.Database.BeginTransactionAsync();

            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(request.Code);
            string incomingCodeHash = Convert.ToBase64String(sha.ComputeHash(bytes));

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
                await tx.CommitAsync();

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
                await tx.CommitAsync();

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
                await tx.CommitAsync();

                return BadRequest("Code expired");
            }

            user.IsEmailConfirmed = true;
            user.EmailConfirmationCodeHash = null;
            user.EmailConfirmationExpires = null;
            user.FailedEmailConfirmAttempts = 0;
            user.LockoutEnd = null;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

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
            return Ok();
        }
    }
}