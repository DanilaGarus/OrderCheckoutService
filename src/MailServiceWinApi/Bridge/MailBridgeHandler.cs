using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using BackendAPI.ExcelFunctions;
using BackendAPI.MailMethods;
using MailServiceWinApi.Models;
using Microsoft.AspNetCore.Http;

namespace MailServiceWinApi.Bridge;

public sealed class MailBridgeHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<BridgeResponse> HandleAsync(BridgeRequest request)
    {
        if (!string.Equals(request.type, "api", StringComparison.OrdinalIgnoreCase))
        {
            return BridgeResponse.For(request.id, 400, """{"message":"Unknown bridge message type."}""");
        }

        try
        {
            var method = request.method.ToUpperInvariant();
            var path = NormalizePath(request.url);

            return (method, path) switch
            {
                ("POST", "/checkClient") => await ExecuteAsync(request.id, await HandleCheckClientAsync(request.body)),
                ("POST", "/convert") => await HandleConvertAsync(request),
                ("POST", "/send") => await ExecuteAsync(request.id, await HandleSendMailAsync(request.body)),
                _ => BridgeResponse.For(request.id, 404, """{"message":"Not found"}"""),
            };
        }
        catch (Exception ex)
        {
            return BridgeResponse.For(request.id, 500, JsonSerializer.Serialize(new { message = ex.Message }));
        }
    }

    private static async Task<BridgeResponse> ExecuteAsync(string requestId, IResult result) =>
        await IResultExecutor.ToBridgeResponseAsync(requestId, result);

    private static async Task<IResult> HandleCheckClientAsync(string? body)
    {
        var auth = DeserializeAuth(body);
        if (!ValidateAuth(auth, out var error))
        {
            return Results.BadRequest(error);
        }

        return await CheckClientConnectivity.CheckClient(
            auth.host,
            auth.port,
            auth.sslTls,
            auth.userName,
            auth.password);
    }

    private static async Task<IResult> HandleSendMailAsync(string? body)
    {
        var auth = DeserializeAuth(body);
        if (auth is null || string.IsNullOrWhiteSpace(auth.to))
        {
            return Results.BadRequest("to is required.");
        }

        if (!ValidateAuth(auth, out var error))
        {
            return Results.BadRequest(error);
        }

        var attachments = auth.attachments?
            .Where(a => !string.IsNullOrWhiteSpace(a.contentBase64))
            .Select(a => new OutgoingAttachment
            {
                FileName = a.fileName,
                ContentBase64 = a.contentBase64,
            })
            .ToList();

        return await SendMessage.SendMail(
            auth.host,
            auth.port,
            auth.sslTls,
            auth.userName,
            auth.password,
            auth.to,
            string.IsNullOrWhiteSpace(auth.cc) ? null : auth.cc,
            auth.subject,
            string.IsNullOrWhiteSpace(auth.textBody) ? null : auth.textBody,
            string.IsNullOrWhiteSpace(auth.htmlBody) ? null : auth.htmlBody,
            string.IsNullOrWhiteSpace(auth.inReplyTo) ? null : auth.inReplyTo,
            attachments);
    }

    private static async Task<BridgeResponse> HandleConvertAsync(BridgeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.body))
        {
            return BridgeResponse.For(request.id, 400, """{"message":"Excel file is required."}""");
        }

        var payload = JsonSerializer.Deserialize<ConvertBridgePayload>(request.body, JsonOptions);
        if (payload?.fileBase64 is null || payload.fileBase64.Length == 0)
        {
            return BridgeResponse.For(request.id, 400, """{"message":"Excel file is required."}""");
        }

        try
        {
            var bytes = Convert.FromBase64String(payload.fileBase64);
            var results = await CsvConvertor.ConvertRowsAsync(bytes, payload.fileName);
            if (results.Count == 0)
            {
                return BridgeResponse.For(request.id, 400, """{"message":"Excel file has no data rows."}""");
            }

            var responseBody = JsonSerializer.Serialize(new
            {
                files = results.Select(r => new
                {
                    fileName = r.FileName,
                    rowNumber = r.RowNumber,
                    contentType = r.ContentType,
                    contentBase64 = Convert.ToBase64String(r.Content),
                }),
            });
            return BridgeResponse.For(request.id, 200, responseBody);
        }
        catch (ArgumentException ex)
        {
            return BridgeResponse.For(request.id, 400, JsonSerializer.Serialize(new { message = ex.Message }));
        }
    }

    private static UserDto? DeserializeAuth(string? body) =>
        string.IsNullOrWhiteSpace(body) ? null : JsonSerializer.Deserialize<UserDto>(body, JsonOptions);

    private static bool ValidateAuth([NotNullWhen(true)] UserDto? auth, out string error)
    {
        if (auth is null
            || string.IsNullOrWhiteSpace(auth.host)
            || string.IsNullOrWhiteSpace(auth.userName)
            || string.IsNullOrWhiteSpace(auth.password)
            || auth.port <= 0)
        {
            error = "Invalid auth payload.";
            return false;
        }

        error = "";
        return true;
    }

    private static string NormalizePath(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute))
        {
            return absolute.AbsolutePath;
        }

        if (Uri.TryCreate("https://local" + (url.StartsWith('/') ? url : "/" + url), UriKind.Absolute, out var relative))
        {
            return relative.AbsolutePath;
        }

        return url;
    }

    private sealed class ConvertBridgePayload
    {
        public string? fileName { get; set; }
        public string? fileBase64 { get; set; }
    }
}
