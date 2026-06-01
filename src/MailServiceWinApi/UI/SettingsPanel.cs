using MailServiceWinApi.Models;

namespace MailServiceWinApi.UI;

public sealed class SettingsPanel : Panel
{
    private readonly TextBox _host = new() { Width = 200 };
    private readonly TextBox _port = new() { Width = 80 };
    private readonly TextBox _userName = new() { Width = 200 };
    private readonly TextBox _password = new() { Width = 200, UseSystemPasswordChar = true };
    private readonly TextBox _recipientEmail = new() { Width = 200 };
    private readonly CheckBox _sslTls = new() { Text = "Use SSL/TLS", AutoSize = true };
    private readonly Button _saveButton = new() { Text = "Сохранить", AutoSize = true };
    private readonly Button _backButton = new() { Text = "Назад", AutoSize = true };

    public event Func<ClientSettings, Task>? SaveRequested;
    public event EventHandler? BackRequested;

    public SettingsPanel()
    {
        Dock = DockStyle.Fill;
        BackColor = AppTheme.PageBackground;
        Padding = new Padding(15);

        AppTheme.StylePrimaryButton(_saveButton);
        AppTheme.StyleSecondaryButton(_backButton);
        _saveButton.Click += async (_, _) => await RaiseSaveAsync();
        _backButton.Click += (_, _) => BackRequested?.Invoke(this, EventArgs.Empty);

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            Padding = new Padding(0),
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        AddRow(form, "Host", _host);
        AddRow(form, "Port", _port);
        AddRow(form, "Username", _userName);
        AddRow(form, "Password", _password);
        AddRow(form, "Почта получателя", _recipientEmail);
        AddRow(form, "", _sslTls);

        var buttonRow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(80, 8, 0, 0) };
        buttonRow.Controls.Add(_saveButton);
        buttonRow.Controls.Add(_backButton);

        Controls.Add(buttonRow);
        Controls.Add(form);
    }

    public void LoadSettings(ClientSettings settings)
    {
        _host.Text = settings.Host;
        _port.Text = settings.Port > 0 ? settings.Port.ToString() : "";
        _userName.Text = settings.UserName;
        _password.Text = settings.Password;
        _recipientEmail.Text = settings.RecipientEmail;
        _sslTls.Checked = settings.SslTls;
    }

    public ClientSettings ReadSettings()
    {
        _ = int.TryParse(_port.Text, out var port);
        return new ClientSettings
        {
            Host = _host.Text.Trim(),
            Port = port,
            UserName = _userName.Text.Trim(),
            Password = _password.Text,
            RecipientEmail = _recipientEmail.Text.Trim(),
            SslTls = _sslTls.Checked,
        };
    }

    private async Task RaiseSaveAsync()
    {
        if (SaveRequested is not null)
        {
            await SaveRequested(ReadSettings());
        }
    }

    private static void AddRow(TableLayoutPanel form, string label, Control input)
    {
        var row = form.RowCount;
        form.RowCount++;
        form.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        if (!string.IsNullOrEmpty(label))
        {
            form.Controls.Add(new Label
            {
                Text = label,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Padding = new Padding(0, 8, 12, 0),
            }, 0, row);
        }

        input.Margin = new Padding(0, 4, 16, 4);
        form.Controls.Add(input, 1, row);
    }
}
