using System.Text.Json;
using MailServiceWinApi.Models;

namespace MailServiceWinApi.Services;

public sealed class ClientSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private readonly string _filePath;

    public ClientSettingsStore()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MailService");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "client.json");
        
    }

    public ClientSettings Load()
    {
        if (!File.Exists(_filePath))
        {
            var defaults = new ClientSettings();
            Save(defaults);
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            var dto = JsonSerializer.Deserialize<PersistedClient>(json, JsonOptions);
            return Sanitize(dto);
        }
        catch
        {
            var defaults = new ClientSettings();
            Save(defaults);
            return defaults;
        }
    }

    public void Save(ClientSettings settings)
    {
        var persisted = new PersistedClient
        {
            host = settings.Host,
            port = settings.Port > 0 ? settings.Port.ToString() : "",
            userName = settings.UserName,
            password = settings.Password,
            sslTls = settings.SslTls,
            recipientEmail = settings.RecipientEmail,
            selectedRecipientOption = settings.SelectedRecipientOption,
            customRecipientEmails = settings.CustomRecipientEmails
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Select(email => email.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            mailSubject = settings.MailSubject,
            brand = settings.Brand,
            startRowNumber = settings.StartRowNumber > 0 ? settings.StartRowNumber.ToString() : "",
            startColumnNumber = settings.StartColumnNumber > 0 ? settings.StartColumnNumber.ToString() : "",
        };
        File.WriteAllText(_filePath, JsonSerializer.Serialize(persisted, JsonOptions));
    }

    private static ClientSettings Sanitize(PersistedClient? dto)
    {
        dto ??= new PersistedClient();
        _ = int.TryParse(dto.port, out var port);
        return new ClientSettings
        {
            Host = dto.host ?? "",
            Port = port > 0 ? port : 0,
            UserName = dto.userName ?? "",
            Password = dto.password ?? "",
            SslTls = dto.sslTls,
            RecipientEmail = ResolveStoredRecipientEmail(dto),
            SelectedRecipientOption = ResolveStoredRecipientOption(dto),
            CustomRecipientEmails = SanitizeCustomRecipientEmails(dto.customRecipientEmails, dto.recipientEmail),
            MailSubject = dto.mailSubject ?? "",
            Brand = dto.brand ?? "",
            StartRowNumber = ParsePositiveInt(dto.startRowNumber, 1),
            StartColumnNumber = ParsePositiveInt(dto.startColumnNumber, 1),
        };
    }

    private static int ParsePositiveInt(string? value, int defaultValue)
    {
        return int.TryParse(value, out var parsed) && parsed > 0 ? parsed : defaultValue;
    }

    private static string ResolveStoredRecipientEmail(PersistedClient dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.selectedRecipientOption))
        {
            return ClientSettings.ResolveRecipientEmail(dto.selectedRecipientOption);
        }

        var legacyEmail = dto.recipientEmail?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(legacyEmail)
            || string.Equals(legacyEmail, ClientSettings.StandardRecipientEmail, StringComparison.OrdinalIgnoreCase))
        {
            return ClientSettings.StandardRecipientEmail;
        }

        return legacyEmail;
    }

    private static string ResolveStoredRecipientOption(PersistedClient dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.selectedRecipientOption))
        {
            return dto.selectedRecipientOption;
        }

        var legacyEmail = dto.recipientEmail?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(legacyEmail)
            || string.Equals(legacyEmail, ClientSettings.StandardRecipientEmail, StringComparison.OrdinalIgnoreCase))
        {
            return ClientSettings.StandardRecipientLabel;
        }

        return legacyEmail;
    }

    private static List<string> SanitizeCustomRecipientEmails(IEnumerable<string>? storedEmails, string? legacyEmail)
    {
        var emails = storedEmails?
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(email => email.Trim())
            .Where(email => !string.Equals(email, ClientSettings.StandardRecipientEmail, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        var legacy = legacyEmail?.Trim() ?? "";
        if (!string.IsNullOrWhiteSpace(legacy)
            && !string.Equals(legacy, ClientSettings.StandardRecipientEmail, StringComparison.OrdinalIgnoreCase)
            && !emails.Contains(legacy, StringComparer.OrdinalIgnoreCase))
        {
            emails.Add(legacy);
        }

        return emails;
    }

    private sealed class PersistedClient
    {
        public string? host { get; set; }
        public string? port { get; set; }
        public string? userName { get; set; }
        public string? password { get; set; }
        public bool sslTls { get; set; }
        public string? recipientEmail { get; set; }
        public string? selectedRecipientOption { get; set; }
        public List<string>? customRecipientEmails { get; set; }
        public string? mailSubject { get; set; }
        public string? brand { get; set; }
        public string? startRowNumber { get; set; }
        public string? startColumnNumber { get; set; }
    }
}
