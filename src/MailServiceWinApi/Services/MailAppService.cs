using System.Text.Json;
using MailServiceWinApi.Bridge;
using MailServiceWinApi.Models;

namespace MailServiceWinApi.Services;

public sealed class MailAppService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly MailBridgeHandler _bridge = new();

    public async Task CheckClientAsync(ClientSettings settings, CancellationToken cancellationToken = default)
    {
        var body = JsonSerializer.Serialize(settings.ToDto(), JsonOptions);
        await CallAsync("POST", "/checkClient", body, cancellationToken);
    }

    public async Task<IReadOnlyList<ConvertedFile>> ConvertExcelRowsAsync(
        byte[] excelBytes,
        string? fileName = null,
        int startRowNumber = 1,
        int startColumnNumber = 1,
        string? brand = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            fileName,
            fileBase64 = Convert.ToBase64String(excelBytes),
            startRowNumber,
            startColumnNumber,
            brand,
        };
        var body = JsonSerializer.Serialize(payload, JsonOptions);
        var response = await _bridge.HandleAsync(new BridgeRequest
        {
            type = "api",
            id = Guid.NewGuid().ToString("N"),
            method = "POST",
            url = "https://local/convert",
            body = body,
        });

        cancellationToken.ThrowIfCancellationRequested();

        if (response.status is < 200 or >= 300)
        {
            var message = TryReadErrorMessage(response.body) ?? $"HTTP {response.status}";
            throw new InvalidOperationException(message);
        }

        if (string.IsNullOrWhiteSpace(response.body))
        {
            throw new InvalidOperationException("Пустой ответ при конвертации файла.");
        }

        using var doc = JsonDocument.Parse(response.body);
        if (!doc.RootElement.TryGetProperty("files", out var filesElement)
            || filesElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Некорректный ответ конвертации.");
        }

        var files = new List<ConvertedFile>();
        foreach (var fileElement in filesElement.EnumerateArray())
        {
            var contentBase64 = fileElement.GetProperty("contentBase64").GetString();
            if (string.IsNullOrEmpty(contentBase64))
            {
                continue;
            }

            files.Add(new ConvertedFile
            {
                Content = Convert.FromBase64String(contentBase64),
                FileName = fileElement.GetProperty("fileName").GetString() ?? "row.csv",
                ContentType = fileElement.TryGetProperty("contentType", out var ct)
                    ? ct.GetString() ?? "text/csv"
                    : "text/csv",
                RowNumber = fileElement.TryGetProperty("rowNumber", out var row) ? row.GetInt32() : files.Count + 1,
            });
        }

        if (files.Count == 0)
        {
            throw new InvalidOperationException("Файл не содержит строк для обработки.");
        }

        return files;
    }

    public async Task SendMailAsync(
        ClientSettings settings,
        string to,
        string subject,
        string textBody,
        IReadOnlyList<string>? attachmentPaths = null,
        CancellationToken cancellationToken = default)
    {
        var dto = settings.ToDto();
        dto.to = to;
        dto.subject = subject;
        dto.textBody = textBody;
        dto.attachments = BuildAttachmentPayloads(attachmentPaths);
        var body = JsonSerializer.Serialize(dto, JsonOptions);
        await CallAsync("POST", "/send", body, cancellationToken);
    }

    private static List<MailAttachmentPayload> BuildAttachmentPayloads(IReadOnlyList<string>? paths)
    {
        if (paths is null || paths.Count == 0)
        {
            return [];
        }

        var list = new List<MailAttachmentPayload>();
        foreach (var path in paths)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                continue;
            }

            var bytes = File.ReadAllBytes(path);
            list.Add(new MailAttachmentPayload
            {
                fileName = Path.GetFileName(path),
                contentBase64 = Convert.ToBase64String(bytes),
            });
        }

        return list;
    }

    private async Task CallAsync(
        string method,
        string path,
        string? body = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _bridge.HandleAsync(new BridgeRequest
        {
            type = "api",
            id = Guid.NewGuid().ToString("N"),
            method = method,
            url = $"https://local{path}",
            body = body,
        });

        cancellationToken.ThrowIfCancellationRequested();

        if (response.status is >= 200 and < 300)
        {
            return;
        }

        var message = TryReadErrorMessage(response.body) ?? $"HTTP {response.status}";
        throw new InvalidOperationException(message);
    }

    private static string? TryReadErrorMessage(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var message))
            {
                return message.GetString();
            }
        }
        catch (JsonException)
        {
            return body;
        }

        return null;
    }
}

public sealed class ConvertedFile
{
    public required byte[] Content { get; init; }
    public required string FileName { get; init; }
    public string ContentType { get; init; } = "text/csv";
    public int RowNumber { get; init; }
}
