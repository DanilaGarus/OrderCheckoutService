namespace MailServiceWinApi.Models;

public class UserDto
{
    public string host { get; set; } = "";
    public int port { get; set; }
    public bool sslTls { get; set; }
    public string userName { get; set; } = "";
    public string password { get; set; } = "";
    public string to { get; set; } = "";
    public string cc { get; set; } = "";
    public string subject { get; set; } = "";
    public string textBody { get; set; } = "";
    public string htmlBody { get; set; } = "";
    public string inReplyTo { get; set; } = "";
    public List<MailAttachmentPayload> attachments { get; set; } = [];
}
