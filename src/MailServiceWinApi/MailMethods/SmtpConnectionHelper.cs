using MailKit.Security;

namespace MailServiceWinApi.MailMethods;

internal static class SmtpConnectionHelper
{
    public static (string Host, int Port, SecureSocketOptions SocketOptions) FromImapHost(
        string imapHost,
        int imapPort,
        bool imapSslTls)
    {
        var host = imapHost.Trim();

        if (host.StartsWith("imap.", StringComparison.OrdinalIgnoreCase))
        {
            host = "smtp." + host[5..];
        }
        else if (!host.StartsWith("smtp.", StringComparison.OrdinalIgnoreCase))
        {
            host = host.Replace("imap", "smtp", StringComparison.OrdinalIgnoreCase);
        }

        var port = imapPort == 993 || imapSslTls ? 587 : 587;
        var socketOptions = imapPort == 993 || imapSslTls
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.StartTlsWhenAvailable;

        return (host, port, socketOptions);
    }
}
