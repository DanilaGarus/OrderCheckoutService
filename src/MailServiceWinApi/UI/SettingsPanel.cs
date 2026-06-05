using MailServiceWinApi.Models;

namespace MailServiceWinApi.UI;

public sealed class SettingsPanel : Panel
{
    private readonly TextBox _host = new() { Width = 250 };
    private readonly TextBox _port = new() { Width = 80 };
    private readonly TextBox _userName = new() { Width = 250 };
    private readonly TextBox _password = new() { Width = 250, UseSystemPasswordChar = true };
    private readonly CheckBox _sslTls = new() { Text = "Use SSL/TLS", AutoSize = true };

    private readonly ComboBox _recipientCombo = new()
    {
        Width = 250,
        DropDownStyle = ComboBoxStyle.DropDownList,
    };
    private readonly TextBox _newRecipientEmail = new() { Width = 170 };
    private readonly Button _addRecipientButton = new() { Text = "Добавить" };
    private readonly List<string> _customRecipients = [];

    private readonly TextBox _brand = new() { Width = 250 };
    private readonly TextBox _startColumn = new() { Width = 120 };
    private readonly TextBox _startRow = new() { Width = 120 };

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
        AppTheme.StyleSecondaryButton(_addRecipientButton);
        _saveButton.Click += async (_, _) => await RaiseSaveAsync();
        _backButton.Click += (_, _) => BackRequested?.Invoke(this, EventArgs.Empty);
        _addRecipientButton.Click += (_, _) => AddRecipientEmail();

        var mailTab = CreateTabPage("Почта");
        var mailForm = CreateForm();
        AddRow(mailForm, "Host", _host);
        AddRow(mailForm, "Port", _port);
        AddRow(mailForm, "Username", _userName);
        AddRow(mailForm, "Password", _password);
        AddRow(mailForm, "", _sslTls);
        mailTab.Controls.Add(mailForm);

        var processingTab = CreateTabPage("Обработка");
        var processingForm = CreateForm();
        AddRow(processingForm, "Почта получателя", CreateRecipientSelector());
        AddRow(processingForm, "Бренд", _brand);
        AddRow(processingForm, "Начальная колонка", _startColumn);
        AddRow(processingForm, "Начальная строка", _startRow);
        processingTab.Controls.Add(processingForm);

        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
        };
        tabControl.TabPages.Add(mailTab);
        tabControl.TabPages.Add(processingTab);

        var buttonRow = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 8, 0, 0),
        };
        buttonRow.Controls.Add(_saveButton);
        buttonRow.Controls.Add(_backButton);

        Controls.Add(tabControl);
        Controls.Add(buttonRow);

        RebuildRecipientCombo();
        SelectRecipientOption(ClientSettings.StandardRecipientLabel);
    }

    public void LoadSettings(ClientSettings settings)
    {
        _host.Text = settings.Host;
        _port.Text = settings.Port > 0 ? settings.Port.ToString() : "";
        _userName.Text = settings.UserName;
        _password.Text = settings.Password;
        _sslTls.Checked = settings.SslTls;

        _customRecipients.Clear();
        _customRecipients.AddRange(settings.CustomRecipientEmails);
        RebuildRecipientCombo();
        SelectRecipientOption(settings.SelectedRecipientOption);

        _brand.Text = settings.Brand;
        _startColumn.Text = settings.StartColumnNumber > 0 ? settings.StartColumnNumber.ToString() : "";
        _startRow.Text = settings.StartRowNumber > 0 ? settings.StartRowNumber.ToString() : "";
    }

    public ClientSettings ReadSettings()
    {
        _ = int.TryParse(_port.Text, out var port);
        var selectedRecipient = _recipientCombo.SelectedItem?.ToString() ?? ClientSettings.StandardRecipientLabel;
        return new ClientSettings
        {
            Host = _host.Text.Trim(),
            Port = port,
            UserName = _userName.Text.Trim(),
            Password = _password.Text,
            SslTls = _sslTls.Checked,
            SelectedRecipientOption = selectedRecipient,
            CustomRecipientEmails = [.._customRecipients],
            RecipientEmail = ClientSettings.ResolveRecipientEmail(selectedRecipient),
            Brand = _brand.Text.Trim(),
            StartColumnNumber = ParsePositiveInt(_startColumn.Text, 1),
            StartRowNumber = ParsePositiveInt(_startRow.Text, 1),
        };
    }

    private Control CreateRecipientSelector()
    {
        var panel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(0, 4, 16, 4),
        };

        panel.Controls.Add(_recipientCombo);

        var addRow = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
        };
        addRow.Controls.Add(_newRecipientEmail);
        addRow.Controls.Add(_addRecipientButton);
        panel.Controls.Add(addRow);

        return panel;
    }

    private void RebuildRecipientCombo()
    {
        var selected = _recipientCombo.SelectedItem?.ToString();
        _recipientCombo.Items.Clear();
        _recipientCombo.Items.Add(ClientSettings.StandardRecipientLabel);

        foreach (var email in _customRecipients.OrderBy(email => email, StringComparer.OrdinalIgnoreCase))
        {
            _recipientCombo.Items.Add(email);
        }

        if (!string.IsNullOrWhiteSpace(selected))
        {
            SelectRecipientOption(selected);
        }
        else if (_recipientCombo.Items.Count > 0)
        {
            _recipientCombo.SelectedIndex = 0;
        }
    }

    private void SelectRecipientOption(string? option)
    {
        if (string.IsNullOrWhiteSpace(option))
        {
            _recipientCombo.SelectedIndex = 0;
            return;
        }

        for (var i = 0; i < _recipientCombo.Items.Count; i++)
        {
            if (string.Equals(_recipientCombo.Items[i]?.ToString(), option, StringComparison.OrdinalIgnoreCase))
            {
                _recipientCombo.SelectedIndex = i;
                return;
            }
        }

        if (string.Equals(option, ClientSettings.StandardRecipientEmail, StringComparison.OrdinalIgnoreCase))
        {
            _recipientCombo.SelectedIndex = 0;
            return;
        }

        if (IsValidEmail(option)
            && !_customRecipients.Contains(option, StringComparer.OrdinalIgnoreCase))
        {
            _customRecipients.Add(option.Trim());
            RebuildRecipientCombo();
            SelectRecipientOption(option);
            return;
        }

        _recipientCombo.SelectedIndex = 0;
    }

    private void AddRecipientEmail()
    {
        var email = _newRecipientEmail.Text.Trim();
        if (!IsValidEmail(email))
        {
            MessageBox.Show(
                "Введите корректный адрес электронной почты.",
                "Почта получателя",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        if (string.Equals(email, ClientSettings.StandardRecipientEmail, StringComparison.OrdinalIgnoreCase))
        {
            SelectRecipientOption(ClientSettings.StandardRecipientLabel);
            _newRecipientEmail.Clear();
            return;
        }

        if (!_customRecipients.Contains(email, StringComparer.OrdinalIgnoreCase))
        {
            _customRecipients.Add(email);
            RebuildRecipientCombo();
        }

        SelectRecipientOption(email);
        _newRecipientEmail.Clear();
    }

    private static bool IsValidEmail(string email) =>
        !string.IsNullOrWhiteSpace(email)
        && email.Contains('@', StringComparison.Ordinal)
        && !email.StartsWith('@')
        && !email.EndsWith('@');

    private async Task RaiseSaveAsync()
    {
        if (SaveRequested is not null)
        {
            await SaveRequested(ReadSettings());
        }
    }

    private static TabPage CreateTabPage(string title) =>
        new()
        {
            Text = title,
            Padding = new Padding(8),
            AutoScroll = true,
        };

    private static TableLayoutPanel CreateForm()
    {
        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            Padding = new Padding(0),
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        return form;
    }

    private static int ParsePositiveInt(string text, int defaultValue)
    {
        return int.TryParse(text.Trim(), out var value) && value > 0 ? value : defaultValue;
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
