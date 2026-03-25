using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using System.Net;
using System.Threading.RateLimiting;
using TelephoneCallRecording.Services.Authorization.Email;
using TelephoneCallRecording.Services.Authorization.Lockout;
using TelephoneCallRecording.Services.Authorization.Lockout.Options;
using TelephoneCallRecording.Services.Authorization.Login;
using TelephoneCallRecording.Services.Authorization.Register;
using TelephoneCallRecording.Services.Cryptography.Authorization;
using TelephoneCallRecording.Services.DataBase.Authorization;


var builder = WebApplication.CreateBuilder(args);

//builder.Services.Configure<ForwardedHeadersOptions>(options =>
//{
//    options.ForwardedHeaders =
//        ForwardedHeaders.XForwardedFor |
//        ForwardedHeaders.XForwardedProto;

//    options.KnownProxies.Add(IPAddress.Parse("127.0.0.1")); // Ставить IP-адреса доверенных прокси, если они есть. В данном случае предполагается, что прокси работает на localhost
//    options.ForwardLimit = 1; // Ограничиваем количество прокси, чтобы предотвратить подделку заголовков.
//});

builder.Services.AddAuthentication("cookie")
    .AddCookie("cookie", options =>
    {
        options.Cookie.Name = "auth_cookie";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;

        options.LoginPath = "/auth/login";
        options.AccessDeniedPath = "/auth/denied";

        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;

        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1)
            }));
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IConfirmationCodeGenerator, ConfirmationCodeGenerator>();
builder.Services.AddScoped<IPasswordValidator, PasswordValidator>();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<IRegisterService, RegisterService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserVerificationService, UserVerificationService>();
builder.Services.AddScoped<IEmailLockoutService, EmailLockoutService>();
builder.Services.AddScoped<ILoginLockoutService, LoginLockoutService>();
builder.Services.AddScoped<IEmailConfirmationValidator, EmailConfirmationValidator>();

builder.Services.AddAuthorization();

builder.Services.AddControllers();

builder.Services.AddOpenApi();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

//app.UseForwardedHeaders(); // Оставь самым первым, чтобы корректно обрабатывать IP и протокол от прокси

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.Run();