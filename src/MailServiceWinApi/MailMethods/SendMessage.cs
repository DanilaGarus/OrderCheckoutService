using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace MailServiceWinApi.MailMethods;

public sealed class OutgoingAttachment
{
    public string FileName { get; set; } = "";
    public string ContentBase64 { get; set; } = "";
}

public static class SendMessage
{
    public static async Task<IResult> SendMail(
        string imapHost,
        int imapPort,
        bool imapSslTls,
        string userName,
        string password,
        string to,
        string? cc,
        string subject,
        string? textBody,
        string? htmlBody,
        string? inReplyTo,
        IReadOnlyList<OutgoingAttachment>? attachments = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(to))
        {
            return Results.BadRequest(new { message = "Поле 'Кому' обязательно для заполнения" });
        }

        try
        {
            var (resolvedHost, resolvedPort, socketOptions) = SmtpConnectionHelper.FromImapHost(
                imapHost,
                imapPort,
                imapSslTls);

            using var client = new SmtpClient();
            await client.ConnectAsync(resolvedHost, resolvedPort, socketOptions, ct);
            await client.AuthenticateAsync(userName, password, ct);

            var message = new MimeMessage();
            message.From.Add(ParseMailbox(userName));
            message.To.AddRange(ParseAddressList(to));
            if (!string.IsNullOrWhiteSpace(cc))
            {
                message.Cc.AddRange(ParseAddressList(cc));
            }

            message.Subject = subject.Trim();

            if (!string.IsNullOrWhiteSpace(inReplyTo))
            {
                var normalized = NormalizeMessageId(inReplyTo);
                message.InReplyTo = normalized;
                message.References.Add(normalized);
            }

            var builder = new BodyBuilder();
            if (!string.IsNullOrWhiteSpace(htmlBody))
            {
                builder.HtmlBody = htmlBody;
                builder.TextBody = string.IsNullOrWhiteSpace(textBody) ? null : textBody;
            }
            else
            {
                builder.TextBody = textBody;
            }

            if (attachments is not null)
            {
                foreach (var attachment in attachments)
                {
                    if (string.IsNullOrWhiteSpace(attachment.ContentBase64))
                    {
                        continue;
                    }

                    var bytes = Convert.FromBase64String(attachment.ContentBase64);
                    var fileName = string.IsNullOrWhiteSpace(attachment.FileName)
                        ? "attachment"
                        : attachment.FileName;
                    builder.Attachments.Add(fileName, bytes);
                }
            }

            message.Body = builder.ToMessageBody();

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            return Results.Ok(new
            {
                message = "Message sent.",
                smtpHost = resolvedHost,
                smtpPort = resolvedPort,
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Send failed",
                detail: $"{ex.Message} (Отправка выполняется через SMTP; IMAP используется только для чтения почты.)",
                statusCode: 500);
        }
    }

    private static MailboxAddress ParseMailbox(string address)
    {
        if (MailboxAddress.TryParse(address, out var parsed))
        {
            return parsed;
        }

        return new MailboxAddress("", address.Trim());
    }

    private static InternetAddressList ParseAddressList(string raw)
    {
        var list = new InternetAddressList();
        foreach (var part in raw.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = part.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            if (MailboxAddress.TryParse(trimmed, out var mailbox))
            {
                list.Add(mailbox);
            }
            else if (trimmed.Contains('@'))
            {
                list.Add(new MailboxAddress("", trimmed));
            }
        }

        return list;
    }

    private static string NormalizeMessageId(string messageId)
    {
        var trimmed = messageId.Trim();
        if (trimmed.StartsWith('<') && trimmed.EndsWith('>'))
        {
            return trimmed;
        }

        return $"<{trimmed.Trim('<', '>')}>";
    }
}
