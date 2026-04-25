using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using System.Net;
using System.Threading.RateLimiting;
using TelephoneCallRecording.Services.Authorization.Email;
using TelephoneCallRecording.Services.Authorization.Lockout;
using TelephoneCallRecording.Services.Authorization.Lockout.Options;
using TelephoneCallRecording.Services.Authorization.Login;
using TelephoneCallRecording.Services.Authorization.Register;
using TelephoneCallRecording.Services.Calls;
using TelephoneCallRecording.Services.Cryptography.Authorization;
using TelephoneCallRecording.Services.DataBase.Authorization;
using TelephoneCallRecording.Services.Security;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment();

builder.Services.AddProblemDetails();
builder.Services.Configure<LockoutOptions>(
    builder.Configuration.GetSection(LockoutOptions.SectionName));
builder.Services.Configure<EmailDeliveryOptions>(
    builder.Configuration.GetSection(EmailDeliveryOptions.SectionName));
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();

    foreach (var proxy in builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [])
    {
        if (IPAddress.TryParse(proxy, out var address))
        {
            options.KnownProxies.Add(address);
        }
    }

    options.ForwardLimit = 1;
});

builder.Services.AddAuthentication("cookie")
    .AddCookie("cookie", options =>
    {
        options.Cookie.Name = isDevelopment ? "auth_cookie" : "__Host-tcr-auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = isDevelopment
            ? CookieSecurePolicy.None
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.Path = "/";

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
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { code = "rate_limit", message = "Слишком много запросов. Повторите позже." },
            cancellationToken: token);
    };

    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 8,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.AddPolicy("calls", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1)
            }));
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Connection string 'Default' is not configured.")));

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = isDevelopment ? "tcr_csrf" : "__Host-tcr-csrf";
    options.Cookie.HttpOnly = false;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = isDevelopment
        ? CookieSecurePolicy.None
        : CookieSecurePolicy.Always;
    options.Cookie.Path = "/";
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var details = context.ModelState.Values
            .SelectMany(x => x.Errors)
            .Select(x => string.IsNullOrWhiteSpace(x.ErrorMessage)
                ? "Одно или несколько полей заполнены некорректно."
                : x.ErrorMessage)
            .Distinct()
            .ToArray();

        return new BadRequestObjectResult(new
        {
            code = "validation_error",
            message = "Проверьте корректность заполнения полей.",
            details
        });
    };
});

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
builder.Services.AddScoped<IVerificationCookieService, VerificationCookieService>();
builder.Services.AddScoped<ICallBillingService, CallBillingService>();
builder.Services.AddScoped<ApiAntiforgeryValidationFilter>();

builder.Services.AddAuthorization();
builder.Services.AddControllers(options =>
{
    options.Filters.AddService<ApiAntiforgeryValidationFilter>();
});
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseHsts();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("GlobalExceptionHandler");
        var feature = context.Features.Get<IExceptionHandlerFeature>();

        if (feature?.Error != null)
        {
            logger.LogError(feature.Error, "Unhandled exception while processing {Method} {Path}.", context.Request.Method, context.Request.Path);
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json; charset=utf-8";

        await context.Response.WriteAsJsonAsync(new
        {
            code = "server_error",
            message = "Внутренняя ошибка сервера."
        });
    });
});

var forwardedHeadersOptions = app.Services
    .GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

app.UseForwardedHeaders(forwardedHeadersOptions);

if (!isDevelopment)
{
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

    var csp = isDevelopment
        ? "default-src 'self' 'unsafe-inline' 'unsafe-eval' data: blob: ws: http: https:; frame-ancestors 'none'; base-uri 'self';"
        : "default-src 'self'; base-uri 'self'; frame-ancestors 'none'; form-action 'self'; connect-src 'self'; img-src 'self' data:; font-src 'self' data:; object-src 'none'; script-src 'self'; style-src 'self' 'unsafe-inline'; upgrade-insecure-requests";

    context.Response.Headers["Content-Security-Policy"] = csp;

    await next();
});

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", async (AppDbContext db, CancellationToken cancellationToken) =>
{
    var canConnect = await db.Database.CanConnectAsync(cancellationToken);
    return canConnect
        ? Results.Ok(new { status = "ok", database = "ok" })
        : Results.Json(new { status = "degraded", database = "unreachable" }, statusCode: StatusCodes.Status503ServiceUnavailable);
});

app.MapControllers();
app.Run();
