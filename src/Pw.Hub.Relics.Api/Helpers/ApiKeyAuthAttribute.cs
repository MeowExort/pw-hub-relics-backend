using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Pw.Hub.Relics.Api.Helpers;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiKeyAuthAttribute : Attribute, IAsyncActionFilter
{
    private const string ApiKeyHeaderName = "X-Api-Key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var endpoint = context.HttpContext.GetEndpoint();
        var skipApiKey = endpoint?.Metadata.GetMetadata<SkipApiKeyAuthAttribute>() != null;
        
        if (skipApiKey)
        {
            await next();
            return;
        }
        
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var apiKey = configuration["ApiKey"];

        if (string.IsNullOrEmpty(apiKey))
        {
            await next();
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            context.Result = new UnauthorizedObjectResult(new { message = "API Key is missing" });
            return;
        }

        if (!apiKey.Equals(extractedApiKey))
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Invalid API Key" });
            return;
        }

        await next();
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class SkipApiKeyAuthAttribute : Attribute
{
}
