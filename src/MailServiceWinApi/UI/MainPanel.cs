namespace MailServiceWinApi.UI;

public sealed class MainPanel : Panel
{
    private readonly Label _fileLabel = new()
    {
        AutoSize = false,
        Height = 48,
        Text = "Файл не выбран",
        TextAlign = ContentAlignment.MiddleLeft,
    };
    private readonly Label _mailSubjectLabel = new()
    {
        AutoSize = true,
        Text = "Тема письма",
    };
    private readonly TextBox _mailSubject = new()
    {
        Dock = DockStyle.Top,
        Multiline = true,
        Height = 80,
    };
    private readonly Button _selectFileButton = new() { Text = "Выбрать файл" };
    private readonly Button _settingsButton = new() { Text = "Настройки" };
    private readonly Button _processButton = new() { Text = "Обработать" };
    private readonly Button _closeButton = new() { Text = "Закрыть" };

    public event EventHandler? SelectFileClicked;
    public event EventHandler? SettingsClicked;
    public event EventHandler? ProcessClicked;
    public event EventHandler? CloseClicked;

    public MainPanel()
    {
        Dock = DockStyle.Fill;
        BackColor = AppTheme.PageBackground;
        Padding = new Padding(24);

        _fileLabel.Dock = DockStyle.Top;
        _fileLabel.ForeColor = AppTheme.TextSecondary;
        _fileLabel.Font = new Font("Segoe UI", 10F);

        _mailSubjectLabel.Dock = DockStyle.Top;
        _mailSubjectLabel.ForeColor = AppTheme.TextPrimary;
        _mailSubjectLabel.Font = new Font("Segoe UI", 9F);
        _mailSubjectLabel.Padding = new Padding(0, 12, 0, 4);

        AppTheme.StylePrimaryButton(_processButton);
        AppTheme.StyleSecondaryButton(_selectFileButton);
        AppTheme.StyleSecondaryButton(_settingsButton);
        AppTheme.StyleSecondaryButton(_closeButton);

        _selectFileButton.Click += (_, _) => SelectFileClicked?.Invoke(this, EventArgs.Empty);
        _settingsButton.Click += (_, _) => SettingsClicked?.Invoke(this, EventArgs.Empty);
        _processButton.Click += (_, _) => ProcessClicked?.Invoke(this, EventArgs.Empty);
        _closeButton.Click += (_, _) => CloseClicked?.Invoke(this, EventArgs.Empty);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 16, 0, 0),
        };
        buttons.Controls.Add(_selectFileButton);
        buttons.Controls.Add(_settingsButton);
        buttons.Controls.Add(_processButton);
        buttons.Controls.Add(_closeButton);

        Controls.Add(buttons);
        Controls.Add(_mailSubject);
        Controls.Add(_mailSubjectLabel);
        Controls.Add(_fileLabel);
    }

    public void SetMailSubject(string subject) => _mailSubject.Text = subject;

    public string ReadMailSubject() => _mailSubject.Text.Trim();

    public void SetSelectedFile(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            _fileLabel.Text = "Файл не выбран";
            _fileLabel.ForeColor = AppTheme.TextSecondary;
            _processButton.Enabled = false;
            return;
        }

        _fileLabel.Text = filePath;
        _fileLabel.ForeColor = AppTheme.TextPrimary;
        _processButton.Enabled = true;
    }

    public void SetBusy(bool busy)
    {
        _selectFileButton.Enabled = !busy;
        _settingsButton.Enabled = !busy;
        _processButton.Enabled = !busy && !string.IsNullOrWhiteSpace(_fileLabel.Text) && _fileLabel.Text != "Файл не выбран";
        _closeButton.Enabled = !busy;
        _mailSubject.Enabled = !busy;
    }
}
