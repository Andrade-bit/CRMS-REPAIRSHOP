using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CRMS.winforms; // reuses RoundedPanel from Login.cs
using CRMS.winforms.Api;
using CRMS.winforms.Controllers;
using CRMS.winforms.Shared;
using RoundedRect = CRMS.winforms.RoundedRect;

namespace CRMS.winforms.SuperAdmin
{
    /// <summary>
    /// Superadmin's Dashboard — restyled to match the CRMS.PlatformConsole
    /// reference design: four KPI cards (Total Companies, Active
    /// Subscriptions, Active Admin Accounts, Revenue 30D) followed by one
    /// summary table (Company / Admin / Billing Cycle / Subscription).
    /// Every number is still a real aggregate from the same endpoints the
    /// full Subscriptions/Reports/Users pages use — only the layout changed.
    /// </summary>
    public partial class SuperadminDashboardControl : UserControl
    {
        private static readonly Color CardBorder = Color.FromArgb(231, 231, 239);
        private static readonly Color Ink = Color.FromArgb(29, 29, 32);
        private static readonly Color Muted = Color.FromArgb(119, 119, 127);
        private static readonly Color GreenBg = Color.FromArgb(218, 245, 231);
        private static readonly Color GreenText = Color.FromArgb(0, 145, 96);

        private readonly SuperAdminController _controller;

        private Label _companiesValue = null!;
        private Label _activeSubsValue = null!;
        private Label _activeAdminsValue = null!;
        private Label _revenueValue = null!;
        private DataGridView _summaryGrid = null!;

        public SuperadminDashboardControl(SuperAdminController controller)
        {
            _controller = controller;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            BackColor = Color.White;
            AutoScroll = true;
            Padding = new Padding(50, 40, 50, 40);

            var cardsRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 146,
                ColumnCount = 4,
                RowCount = 1,
                Margin = Padding.Empty
            };
            for (int i = 0; i < 4; i++)
                cardsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            cardsRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            (RoundedPanel Card, Label Value) BuildCard(string caption, Color accent, int col)
            {
                var card = new RoundedPanel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    BorderColor = CardBorder,
                    CornerRadius = 18,
                    Margin = new Padding(col == 0 ? 0 : 11, 0, col == 3 ? 0 : 11, 0)
                };
                var bar = new Panel { Size = new Size(45, 5), Location = new Point(25, 22), BackColor = accent };
                var value = new Label
                {
                    Text = "0",
                    Font = new Font("Segoe UI", 27F, FontStyle.Bold),
                    ForeColor = Ink,
                    AutoSize = true,
                    Location = new Point(25, 47)
                };
                var cap = new Label
                {
                    Text = caption,
                    Font = new Font("Segoe UI", 10F),
                    ForeColor = Muted,
                    AutoSize = true,
                    Location = new Point(25, 98)
                };
                card.Controls.Add(cap);
                card.Controls.Add(value);
                card.Controls.Add(bar);
                cardsRow.Controls.Add(card, col, 0);
                return (card, value);
            }

            (_, _companiesValue) = BuildCard("TOTAL COMPANIES", Theme.Accent, 0);
            (_, _activeSubsValue) = BuildCard("ACTIVE SUBSCRIPTIONS", Color.FromArgb(24, 133, 81), 1);
            (_, _activeAdminsValue) = BuildCard("ACTIVE ADMIN ACCOUNTS", Color.FromArgb(35, 87, 178), 2);
            (_, _revenueValue) = BuildCard("REVENUE (30D)", Color.FromArgb(155, 111, 8), 3);

            var spacer = Theme.CreateSpacer(24);
            spacer.BackColor = Color.White;

            var tableShell = new RoundedPanel
            {
                Dock = DockStyle.Top,
                Height = 320,
                BackColor = Color.FromArgb(247, 248, 250),
                BorderColor = CardBorder,
                CornerRadius = 15,
                Padding = new Padding(1)
            };

            _summaryGrid = Theme.CreateStyledGrid();
            _summaryGrid.RowTemplate.Height = 64;
            _summaryGrid.Columns.Add("Company", "Company");
            _summaryGrid.Columns.Add("Admin", "Admin");
            _summaryGrid.Columns.Add("Cycle", "Billing Cycle");
            _summaryGrid.Columns.Add("Status", "Subscription");
            _summaryGrid.Columns["Company"].FillWeight = 35;
            _summaryGrid.Columns["Admin"].FillWeight = 30;
            _summaryGrid.Columns["Cycle"].FillWeight = 18;
            _summaryGrid.Columns["Status"].FillWeight = 17;
            foreach (DataGridViewColumn column in _summaryGrid.Columns)
                column.ReadOnly = true;

            _summaryGrid.CellPainting += SummaryGrid_CellPainting;

            ShowEmptyState();
            tableShell.Controls.Add(_summaryGrid);

            Controls.Add(tableShell);
            Controls.Add(spacer);
            Controls.Add(cardsRow);
            Controls.Add(Theme.CreatePageHeader("Dashboard", "An overview of the companies on your platform", showTitle: false));

            ResumeLayout(false);
        }

        private void ShowEmptyState()
        {
            _summaryGrid.Rows.Clear();
            _summaryGrid.Rows.Add("No tenants onboarded yet.", "", "", "");
            _summaryGrid.Rows[0].ReadOnly = true;
            _summaryGrid.Rows[0].DefaultCellStyle = new DataGridViewCellStyle
            {
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 9F, FontStyle.Italic)
            };
        }

        /// <summary>Draws the Subscription column as a colored pill, matching
        /// the Active/Suspended styling used elsewhere in the app.</summary>
        private void SummaryGrid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_summaryGrid.Columns[e.ColumnIndex].Name != "Status") return;

            string status = e.Value?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(status)) return;

            bool active = status == "Active";
            var bg = active ? GreenBg : Theme.StatusUnrepairableBg;
            var fg = active ? GreenText : Theme.StatusUnrepairableText;

            e.PaintBackground(e.CellBounds, true);
            e.Graphics!.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var pillRect = Rectangle.Inflate(e.CellBounds, -30, -14);
            pillRect.Width = Math.Min(pillRect.Width, 90);
            using (var path = RoundedRect.Path(pillRect, pillRect.Height / 2))
            using (var brush = new SolidBrush(bg))
                e.Graphics.FillPath(brush, path);

            TextRenderer.DrawText(e.Graphics, status, new Font("Segoe UI", 8.5F, FontStyle.Bold), pillRect, fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            e.Handled = true;
        }

        /// <summary>Reloads every figure from the real APIs. Call on every page navigation.</summary>
        public async System.Threading.Tasks.Task LoadData()
        {
            var subscriptionsTask = _controller.GetSubscriptionsAsync();
            var reportTask = _controller.GetPlatformReportAsync();
            var adminsTask = _controller.GetAdminAccountsAsync();

            await System.Threading.Tasks.Task.WhenAll(subscriptionsTask, reportTask, adminsTask);

            var subscriptions = subscriptionsTask.Result;
            var report = reportTask.Result;
            var admins = adminsTask.Result;

            _companiesValue.Text = subscriptions.Count.ToString();
            _activeSubsValue.Text = subscriptions.Count(t => t.Status == "Active").ToString();
            _activeAdminsValue.Text = admins.Count(a => a.Status == "Active").ToString();
            // A space after the peso sign — without it, Segoe UI Bold at this
            // size renders "₱" tight enough against the following digit that
            // they visually overlap on some displays.
            _revenueValue.Text = (report?.Revenue30d ?? 0).ToString("₱ #,##0", System.Globalization.CultureInfo.InvariantCulture);

            _summaryGrid.Rows.Clear();
            if (subscriptions.Count == 0)
            {
                ShowEmptyState();
                return;
            }

            foreach (var tenant in subscriptions)
                _summaryGrid.Rows.Add(tenant.CompanyName, tenant.AdminName, tenant.BillingCycle, tenant.Status);
        }
    }
}
