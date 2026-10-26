using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace OrderService.Security;

// Protege os endpoints internos: o pedido tem de trazer a chave X-Internal-Key
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class InternalApiKeyAttribute : Attribute, IAuthorizationFilter
{
    public const string HeaderName = "X-Internal-Key";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var expected = Environment.GetEnvironmentVariable("INTERNAL_API_KEY");
        if (string.IsNullOrWhiteSpace(expected))
        {
            // Se a chave não estiver configurada, o endpoint fica fechado
            context.Result = new ObjectResult(new { Message = "Comunicação interna não configurada." }) { StatusCode = 503 };
            return;
        }

        var provided = context.HttpContext.Request.Headers[HeaderName].ToString();
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided);

        // Comparar em tempo constante para não dar pistas sobre a chave
        if (providedBytes.Length != expectedBytes.Length ||
            !CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes))
        {
            context.Result = new UnauthorizedResult();
        }
    }
}
