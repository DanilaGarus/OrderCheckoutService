using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace MailServiceWinApi.Bridge;

internal static class IResultExecutor
{
    private static readonly IServiceProvider HttpServices = CreateHttpServices();

    private static IServiceProvider CreateHttpServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.ConfigureHttpJsonOptions(_ => { });
        return services.BuildServiceProvider();
    }

    public static async Task<BridgeResponse> ToBridgeResponseAsync(string requestId, IResult result)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = HttpServices,
            Response =
            {
                Body = new MemoryStream()
            }
        };

        await result.ExecuteAsync(context);

        var status = context.Response.StatusCode;
        if (!context.Response.Body.CanSeek)
        {
            var copy = new MemoryStream();
            await context.Response.Body.CopyToAsync(copy);
            copy.Position = 0;
            context.Response.Body = copy;
        }
        else
        {
            context.Response.Body.Position = 0;
        }

        var contentType = context.Response.ContentType ?? "application/json";
        if (IsBinaryContent(contentType))
        {
            using var ms = new MemoryStream();
            await context.Response.Body.CopyToAsync(ms);
            var fileName = context.Response.Headers.ContentDisposition.ToString();
            return BridgeResponse.Binary(requestId, status, ms.ToArray(), contentType, ExtractFileName(fileName));
        }

        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();
        return BridgeResponse.For(requestId, status, string.IsNullOrEmpty(body) ? null : body, contentType);
    }

    private static bool IsBinaryContent(string contentType)
    {
        return !contentType.Contains("json", StringComparison.OrdinalIgnoreCase)
               && !contentType.Contains("text/plain", StringComparison.OrdinalIgnoreCase)
               && !contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ExtractFileName(string contentDisposition)
    {
        if (string.IsNullOrWhiteSpace(contentDisposition))
        {
            return null;
        }

        const string marker = "filename=";
        var index = contentDisposition.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return null;
        }

        return contentDisposition[(index + marker.Length)..].Trim('"', ' ', ';');
    }
}
