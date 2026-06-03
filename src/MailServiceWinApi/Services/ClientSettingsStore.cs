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
            RecipientEmail = dto.recipientEmail ?? "",
        };
    }

    private sealed class PersistedClient
    {
        public string? host { get; set; }
        public string? port { get; set; }
        public string? userName { get; set; }
        public string? password { get; set; }
        public bool sslTls { get; set; }
        public string? recipientEmail { get; set; }
    }
}
