using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS.winforms.Controllers;
using CRMS.winforms.Shared;

namespace CRMS.winforms.SuperAdmin
{
    /// <summary>
    /// Superadmin's Platform Reports page: real cross-tenant aggregates for the
    /// last 30 days, plus a per-tenant revenue/tickets breakdown table.
    /// </summary>
    public partial class AnalyticsReportControl : UserControl
    {
        private readonly SuperAdminController _controller;

        private StatCard cardTickets = null!;
        private StatCard cardRevenue = null!;
        private StatCard cardStaff = null!;
        private StatCard cardRating = null!;
        private DataGridView tenantGrid = null!;

        public AnalyticsReportControl(SuperAdminController controller)
        {
            _controller = controller;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            BackColor = Theme.PageBg;
            Padding = new Padding(Theme.PageMarginX, Theme.PageMarginTop, Theme.PageMarginX, Theme.PageMarginTop);

            var statsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 130,
                BackColor = Theme.PageBg,
                WrapContents = false
            };

            cardTickets = new StatCard("Tickets Resolved (30d)", "0", Theme.StatusRepairingText) { Size = new Size(230, 120), Margin = new Padding(0, 0, 16, 0) };
            cardRevenue = new StatCard("Revenue (30d)", "₱0.00", Theme.Accent) { Size = new Size(230, 120), Margin = new Padding(0, 0, 16, 0) };
            cardStaff = new StatCard("Active Staff Accounts", "0", Theme.StatusRepairedText) { Size = new Size(230, 120), Margin = new Padding(0, 0, 16, 0) };
            cardRating = new StatCard("Average Rating", "—", Theme.StatusDiagnoseText) { Size = new Size(230, 120) };

            statsPanel.Controls.Add(cardTickets);
            statsPanel.Controls.Add(cardRevenue);
            statsPanel.Controls.Add(cardStaff);
            statsPanel.Controls.Add(cardRating);

            var spacer = Theme.CreateSpacer(20);

            var tableCard = Theme.CreateTableCard();

            tenantGrid = Theme.CreateStyledGrid();
            tenantGrid.RowTemplate.Height = 48;
            tenantGrid.Columns.Add("Company", "Company");
            tenantGrid.Columns.Add("Tickets", "Tickets Resolved (30d)");
            tenantGrid.Columns.Add("Revenue", "Revenue (30d)");
            tenantGrid.Columns["Company"].FillWeight = 200;
            tenantGrid.Columns["Tickets"].FillWeight = 120;
            tenantGrid.Columns["Revenue"].FillWeight = 120;
            foreach (DataGridViewColumn column in tenantGrid.Columns)
                column.ReadOnly = true;

            ShowEmptyState();
            tableCard.Controls.Add(tenantGrid);

            Controls.Add(tableCard);
            Controls.Add(spacer);
            Controls.Add(statsPanel);
            Controls.Add(Theme.CreatePageHeader("Platform Reports", "Cross-tenant activity across the last 30 days", showTitle: false, showSubtitle: false));

            ResumeLayout(false);
        }

        private void ShowEmptyState()
        {
            tenantGrid.Rows.Add("No tenant activity yet.", "", "");
            tenantGrid.Rows[0].ReadOnly = true;
            tenantGrid.Rows[0].DefaultCellStyle = new DataGridViewCellStyle
            {
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 9.75F, FontStyle.Italic)
            };
        }

        /// <summary>Recomputes every figure from the real platform report. Call on every page navigation.</summary>
        public async System.Threading.Tasks.Task LoadData()
        {
            var report = await _controller.GetPlatformReportAsync();

            tenantGrid.Rows.Clear();

            if (report is null)
            {
                cardTickets.SetValue("0");
                cardRevenue.SetValue("₱0.00");
                cardStaff.SetValue("0");
                cardRating.SetValue("—");
                ShowEmptyState();
                return;
            }

            cardTickets.SetValue(report.TicketsResolved30d.ToString());
            cardRevenue.SetValue(report.Revenue30d.ToString("₱#,##0.00"));
            cardStaff.SetValue(report.ActiveStaffAccounts.ToString());
            cardRating.SetValue(report.RatingCount > 0 ? $"{report.AverageRating:0.0} ★ ({report.RatingCount})" : "—");

            if (report.ByTenant.Count == 0)
            {
                ShowEmptyState();
                return;
            }

            foreach (var tenant in report.ByTenant)
            {
                tenantGrid.Rows.Add(tenant.CompanyName, tenant.TicketsResolved.ToString(), tenant.Revenue.ToString("₱#,##0.00"));
            }
        }
    }
}
