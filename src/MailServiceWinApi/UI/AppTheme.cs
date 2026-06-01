namespace MailServiceWinApi.UI;

internal static class AppTheme
{
    public static readonly Color PageBackground = Color.FromArgb(246, 248, 251);
    public static readonly Color CardBackground = Color.White;
    public static readonly Color Border = Color.FromArgb(229, 231, 235);
    public static readonly Color TextPrimary = Color.FromArgb(31, 35, 40);
    public static readonly Color TextSecondary = Color.FromArgb(107, 114, 128);
    public static readonly Color Primary = Color.FromArgb(51, 109, 202);
    public static readonly Color PrimaryHover = Color.FromArgb(41, 93, 175);
    public static readonly Color SidebarBackground = Color.White;

    public static void StylePrimaryButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = Primary;
        button.ForeColor = Color.White;
        button.Cursor = Cursors.Hand;
        button.AutoSize = true;
        button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        button.MinimumSize = new Size(88, 34);
        button.Padding = new Padding(14, 6, 14, 6);
        button.Margin = new Padding(0, 0, 8, 0);
        button.UseCompatibleTextRendering = true;
    }

    public static void StyleSecondaryButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.BorderSize = 1;
        button.BackColor = CardBackground;
        button.ForeColor = TextPrimary;
        button.Cursor = Cursors.Hand;
        button.AutoSize = true;
        button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        button.MinimumSize = new Size(88, 34);
        button.Padding = new Padding(14, 6, 14, 6);
        button.Margin = new Padding(0, 0, 8, 0);
        button.UseCompatibleTextRendering = true;
    }

    public static void StyleDataGrid(DataGridView grid)
    {
        grid.BackgroundColor = CardBackground;
        grid.BorderStyle = BorderStyle.FixedSingle;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        grid.EnableHeadersVisualStyles = false;
        grid.GridColor = Border;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = true;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.AutoGenerateColumns = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        grid.ColumnHeadersHeight = 36;
        grid.DefaultCellStyle.BackColor = CardBackground;
        grid.DefaultCellStyle.ForeColor = TextPrimary;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(230, 240, 255);
        grid.DefaultCellStyle.SelectionForeColor = TextPrimary;
        grid.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 251, 252);
        grid.RowTemplate.Height = 52;
    }
}
