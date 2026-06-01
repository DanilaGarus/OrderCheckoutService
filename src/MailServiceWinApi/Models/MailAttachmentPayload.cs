namespace MailServiceWinApi.Models;

public sealed class MailAttachmentPayload
{
    public string fileName { get; set; } = "";
    public string contentBase64 { get; set; } = "";
}
