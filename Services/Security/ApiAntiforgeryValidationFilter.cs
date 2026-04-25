using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TelephoneCallRecording.Services.Security;

public sealed class ApiAntiforgeryValidationFilter : IAsyncAuthorizationFilter
{
    private readonly IAntiforgery _antiforgery;

    public ApiAntiforgeryValidationFilter(IAntiforgery antiforgery)
    {
        _antiforgery = antiforgery;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var request = context.HttpContext.Request;
        if (HttpMethods.IsGet(request.Method) ||
            HttpMethods.IsHead(request.Method) ||
            HttpMethods.IsOptions(request.Method) ||
            HttpMethods.IsTrace(request.Method))
        {
            return;
        }

        if (context.Filters.OfType<IgnoreAntiforgeryTokenAttribute>().Any())
        {
            return;
        }

        try
        {
            await _antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            context.Result = new BadRequestObjectResult(new
            {
                code = "csrf_invalid",
                message = "Токен CSRF недействителен."
            });
        }
    }
}
