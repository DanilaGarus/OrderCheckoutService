namespace MailServiceWinApi.Models;

public sealed class ClientSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; }
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public bool SslTls { get; set; }
    public string RecipientEmail { get; set; } = "";

    public UserDto ToDto() => new()
    {
        host = Host,
        port = Port,
        userName = UserName,
        password = Password,
        sslTls = SslTls,
    };
}
