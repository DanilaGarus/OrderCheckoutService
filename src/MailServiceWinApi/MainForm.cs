using MailServiceWinApi.Models;
using MailServiceWinApi.Services;
using MailServiceWinApi.UI;

namespace MailServiceWinApi;

public sealed class MainForm : Form
{
    private readonly ClientSettingsStore _settingsStore = new();
    private readonly MailAppService _mailService = new();
    private ClientSettings _settings = new();

    private readonly MainPanel _mainPanel = new();
    private readonly SettingsPanel _settingsPanel = new();
    private readonly Panel _contentHost = new() { Dock = DockStyle.Fill };

    private string? _selectedFilePath;

    public MainForm()
    {
        Text = "Сервис обработки";
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = AppTheme.PageBackground;
        MinimumSize = new Size(480, 400);
        ClientSize = new Size(520, 500);

        _settings = _settingsStore.Load();

        _mainPanel.SelectFileClicked += (_, _) => SelectFile();
        _mainPanel.SettingsClicked += (_, _) => ShowSettings();
        _mainPanel.ProcessClicked += async (_, _) => await ProcessFileAsync();
        _mainPanel.CloseClicked += (_, _) => Close();

        _settingsPanel.SaveRequested += async settings => await SaveSettingsAsync(settings);
        _settingsPanel.BackRequested += (_, _) => ShowMain();

        _contentHost.Controls.Add(_mainPanel);
        Controls.Add(_contentHost);

        _mainPanel.SetSelectedFile(null);
    }

    private void SelectFile()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Выберите файл Excel",
            Filter = "Excel files (*.xlsx;*.xls)|*.xlsx;*.xls|All files (*.*)|*.*",
            CheckFileExists = true,
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _selectedFilePath = dialog.FileName;
        _mainPanel.SetSelectedFile(_selectedFilePath);
    }

    private void ShowMain()
    {
        _settingsPanel.LoadSettings(_settings);
        _contentHost.Controls.Clear();
        _contentHost.Controls.Add(_mainPanel);
        _mainPanel.SetSelectedFile(_selectedFilePath);
    }

    private void ShowSettings()
    {
        _settingsPanel.LoadSettings(_settings);
        _contentHost.Controls.Clear();
        _contentHost.Controls.Add(_settingsPanel);
    }

    private async Task ProcessFileAsync()
    {
        if (string.IsNullOrWhiteSpace(_selectedFilePath) || !File.Exists(_selectedFilePath))
        {
            MessageBox.Show(
                "Сначала выберите файл.",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        if (string.IsNullOrWhiteSpace(_settings.RecipientEmail))
        {
            MessageBox.Show(
                "Укажите почту получателя в настройках.",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            ShowSettings();
            return;
        }

        if (string.IsNullOrWhiteSpace(_settings.Host)
            || string.IsNullOrWhiteSpace(_settings.UserName)
            || string.IsNullOrWhiteSpace(_settings.Password)
            || _settings.Port <= 0)
        {
            MessageBox.Show(
                "Заполните настройки подключения к почте.",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            ShowSettings();
            return;
        }

        _mainPanel.SetBusy(true);
        var tempAttachmentPaths = new List<string>();

        try
        {
            var excelBytes = await File.ReadAllBytesAsync(_selectedFilePath);
            var convertedRows = await _mailService.ConvertExcelRowsAsync(
                excelBytes,
                Path.GetFileName(_selectedFilePath));

            var sourceName = Path.GetFileName(_selectedFilePath);
            var sentCount = 0;

            foreach (var row in convertedRows)
            {
                var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}_{row.FileName}");
                await File.WriteAllBytesAsync(tempPath, row.Content);
                tempAttachmentPaths.Add(tempPath);

                await _mailService.SendMailAsync(
                    _settings,
                    _settings.RecipientEmail,
                    $"",
                    $"",
                    attachmentPaths: [tempPath]);

                sentCount++;
            }

            MessageBox.Show(
                $"Отправлено сообщений: {sentCount} на {_settings.RecipientEmail}.",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Не удалось обработать файл: {ex.Message}",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _mainPanel.SetBusy(false);
            _mainPanel.SetSelectedFile(_selectedFilePath);

            foreach (var tempPath in tempAttachmentPaths)
            {
                if (!File.Exists(tempPath))
                {
                    continue;
                }

                try
                {
                    File.Delete(tempPath);
                }
                catch
                {
                    // ignore cleanup errors
                }
            }
        }
    }

    private async Task SaveSettingsAsync(ClientSettings settings)
    {
        _settings = settings;
        _settingsStore.Save(_settings);

        try
        {
            await _mailService.CheckClientAsync(_settings);
            MessageBox.Show(
                "Настройки сохранены. Подключение успешно.",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Настройки сохранены, но подключение не удалось: {ex.Message}",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        ShowMain();
    }
}
