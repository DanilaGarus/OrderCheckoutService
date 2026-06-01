namespace MailServiceWinApi.Bridge;

public sealed class BridgeRequest
{
    public string type { get; set; } = "";
    public string id { get; set; } = "";
    public string method { get; set; } = "";
    public string url { get; set; } = "";
    public string? body { get; set; }
}

public sealed class BridgeResponse
{
    public string type { get; set; } = "api-response";
    public string id { get; set; } = "";
    public int status { get; set; }
    public string contentType { get; set; } = "application/json";
    public string? body { get; set; }
    public string? bodyBase64 { get; set; }
    public string? fileName { get; set; }

    public static BridgeResponse For(string requestId, int status, string? body, string contentType = "application/json")
    {
        return new BridgeResponse
        {
            id = requestId,
            status = status,
            body = body,
            contentType = contentType
        };
    }

    public static BridgeResponse Binary(string requestId, int status, byte[] data, string contentType, string? fileName)
    {
        return new BridgeResponse
        {
            id = requestId,
            status = status,
            bodyBase64 = Convert.ToBase64String(data),
            contentType = contentType,
            fileName = fileName
        };
    }
}
