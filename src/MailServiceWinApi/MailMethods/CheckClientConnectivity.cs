using MailKit.Net.Imap;

namespace MailServiceWinApi.MailMethods;

public class CheckClientConnectivity
{
    public static async Task<IResult> CheckClient(string host, int port, bool sslTls, string userName, string password)
    {
        using var client = new ImapClient();
        await client.ConnectAsync(host, port, sslTls);
        await client.AuthenticateAsync(userName, password);

        if (client.IsConnected && client.IsAuthenticated)
        {
            return Results.Ok();
        }

        return Results.Unauthorized();
    }
}
