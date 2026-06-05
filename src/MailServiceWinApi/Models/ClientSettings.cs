namespace MailServiceWinApi.Models;

public sealed class ClientSettings
{
    public const string StandardRecipientLabel = "Стандартная";
    public const string StandardRecipientEmail = "request@motex.by";

    public string Host { get; set; } = "";
    public int Port { get; set; }
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public bool SslTls { get; set; }
    public string RecipientEmail { get; set; } = StandardRecipientEmail;
    public string SelectedRecipientOption { get; set; } = StandardRecipientLabel;
    public List<string> CustomRecipientEmails { get; set; } = [];
    public string MailSubject { get; set; } = "";
    public string Brand { get; set; } = "";
    public int StartRowNumber { get; set; } = 1;
    public int StartColumnNumber { get; set; } = 1;

    public static string ResolveRecipientEmail(string selectedOption) =>
        string.Equals(selectedOption, StandardRecipientLabel, StringComparison.Ordinal)
            ? StandardRecipientEmail
            : selectedOption.Trim();

    public UserDto ToDto() => new()
    {
        host = Host,
        port = Port,
        userName = UserName,
        password = Password,
        sslTls = SslTls,
    };
}
