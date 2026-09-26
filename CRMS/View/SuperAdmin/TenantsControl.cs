using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CRMS.winforms.Api;
using CRMS.winforms.Controllers;
using CRMS.winforms.Shared;

namespace CRMS.winforms.SuperAdmin
{
    /// <summary>
    /// Superadmin's Subscriptions page: every tenant onboarded onto the platform,
    /// its owning Admin account, its billing details, and a status dropdown to
    /// activate or suspend its subscription. Suspending flips the Admin's real
    /// login access — the same mechanism used for Staff/Manager suspension.
    /// Billing cycle / next billing date / amount are all edited together via
    /// the "Subscribe" button (SubscriptionEditDialog), not inline in the grid —
    /// this never creates a new tenant, only edits an already-onboarded one.
    /// </summary>
    public partial class TenantsControl : UserControl
    {
        private readonly SuperAdminController _controller;

        private DataGridView tenantGrid = null!;
        private Button subscribeBtn = null!;
        private Label toastLabel = null!;
        private System.Windows.Forms.Timer toastTimer = null!;
        private List<TenantSubscriptionEntryDto> _tenants = new();

        private static readonly string[] StatusOptions = { "Active", "Suspended" };

        public TenantsControl(SuperAdminController controller)
        {
            _controller = controller;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            BackColor = Theme.PageBg;
            Padding = new Padding(Theme.PageMarginX, Theme.PageMarginTop, Theme.PageMarginX, Theme.PageMarginTop);

            subscribeBtn = new Button
            {
                Text = "＋  Subscribe",
                Width = 150,
                Height = 38,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Accent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            subscribeBtn.FlatAppearance.BorderSize = 0;
            subscribeBtn.FlatAppearance.MouseOverBackColor = Theme.AccentHover;
            subscribeBtn.FlatAppearance.MouseDownBackColor = Theme.AccentPressed;
            Theme.ApplyRoundedRegion(subscribeBtn, 8);
            subscribeBtn.Click += SubscribeBtn_Click;

            var tableCard = Theme.CreateTableCard();

            tenantGrid = Theme.CreateStyledGrid();
            tenantGrid.RowTemplate.Height = 52;

            tenantGrid.Columns.Add("CompanyName", "Company");
            tenantGrid.Columns.Add("AdminName", "Admin");
            tenantGrid.Columns.Add("Username", "Username");
            tenantGrid.Columns.Add("Onboarded", "Onboarded");
            tenantGrid.Columns.Add("Plan", "Plan");
            tenantGrid.Columns.Add("BillingCycle", "Billing Cycle");
            tenantGrid.Columns.Add("Amount", "Amount");
            tenantGrid.Columns.Add("NextBilling", "Next Billing");

            var statusColumn = new DataGridViewComboBoxColumn
            {
                Name = "Status",
                HeaderText = "Subscription",
                DataSource = StatusOptions,
                FlatStyle = FlatStyle.Flat
            };
            tenantGrid.Columns.Add(statusColumn);

            tenantGrid.Columns["CompanyName"].FillWeight = 150;
            tenantGrid.Columns["AdminName"].FillWeight = 130;
            tenantGrid.Columns["Username"].FillWeight = 110;
            tenantGrid.Columns["Onboarded"].FillWeight = 90;
            tenantGrid.Columns["Plan"].FillWeight = 90;
            tenantGrid.Columns["BillingCycle"].FillWeight = 90;
            tenantGrid.Columns["Amount"].FillWeight = 90;
            tenantGrid.Columns["NextBilling"].FillWeight = 90;
            tenantGrid.Columns["Status"].FillWeight = 100;

            // Everything but Status is display-only here — billing cycle, next
            // billing date, and amount are all set together via the Subscribe
            // dialog instead of edited cell-by-cell in the grid.
            foreach (DataGridViewColumn column in tenantGrid.Columns)
            {
                if (column.Name == "Status") continue;
                column.ReadOnly = true;
            }

            tenantGrid.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (tenantGrid.IsCurrentCellDirty)
                    tenantGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            tenantGrid.CellValueChanged += TenantGrid_CellValueChanged;

            ShowEmptyState();

            tableCard.Controls.Add(tenantGrid);

            toastLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 0,
                BackColor = Theme.StatusRepairedBg,
                ForeColor = Theme.StatusRepairedText,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };
            toastTimer = new System.Windows.Forms.Timer { Interval = 3000 };
            toastTimer.Tick += (s, e) => { toastTimer.Stop(); HideToast(); };

            Controls.Add(tableCard);
            Controls.Add(toastLabel);
            Controls.Add(Theme.CreateSpacer());
            Controls.Add(Theme.CreatePageHeader(
                "Subscriptions", "Tenants onboarded onto the platform and their billing status", subscribeBtn, showTitle: false, showSubtitle: false));

            ResumeLayout(false);
        }

        private void ShowEmptyState()
        {
            // Combo-box columns can't take an empty-string cell value, so that
            // cell is left null instead.
            tenantGrid.Rows.Add("No tenants onboarded yet.", "", "", "", "", "", "", "", null);
            tenantGrid.Rows[0].ReadOnly = true;
            tenantGrid.Rows[0].DefaultCellStyle = new DataGridViewCellStyle
            {
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 9.75F, FontStyle.Italic)
            };
        }

        /// <summary>Reloads the grid from the real API. Call on every page navigation.</summary>
        public async System.Threading.Tasks.Task LoadData()
        {
            tenantGrid.CellValueChanged -= TenantGrid_CellValueChanged;

            _tenants = await _controller.GetSubscriptionsAsync();

            // Cleared right before repopulating (not before the await above) so
            // that if this page gets navigated to again while a previous
            // LoadData() call is still in flight, whichever call's fetch
            // resolves last always leaves the grid showing exactly one copy of
            // the current data instead of appending duplicate rows on top of
            // whatever an earlier, still-pending call already added.
            tenantGrid.Rows.Clear();

            if (_tenants.Count == 0)
            {
                ShowEmptyState();
            }
            else
            {
                foreach (var tenant in _tenants)
                {
                    int rowIndex = tenantGrid.Rows.Add(
                        tenant.CompanyName,
                        tenant.AdminName,
                        tenant.AdminUsername,
                        tenant.CompanyCreatedAt.ToString("MMM d, yyyy"),
                        PlanDisplayName(tenant.Plan),
                        tenant.BillingCycle,
                        tenant.Amount.HasValue ? tenant.Amount.Value.ToString("₱#,##0.00") : "—",
                        tenant.NextBillingDate?.ToString("MMM d, yyyy") ?? "—",
                        tenant.Status);

                    tenantGrid.Rows[rowIndex].Tag = tenant;
                }
            }

            tenantGrid.CellValueChanged += TenantGrid_CellValueChanged;
        }

        private async void SubscribeBtn_Click(object? sender, EventArgs e)
        {
            if (_tenants.Count == 0)
            {
                MessageBox.Show(FindForm(), "No tenants are onboarded yet — there's nothing to subscribe.",
                    "No Tenants", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new SubscriptionEditDialog(_tenants);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

            subscribeBtn.Enabled = false;
            try
            {
                var (cycleSuccess, cycleMessage) = await _controller.SetBillingCycleAsync(
                    dialog.CompanyIdValue, dialog.BillingCycleValue, dialog.NextBillingDateValue);
                if (!cycleSuccess)
                {
                    MessageBox.Show(FindForm(), $"Could not update the billing cycle:\n{cycleMessage}",
                        "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var (amountSuccess, amountMessage) = await _controller.SetSubscriptionAmountAsync(
                    dialog.CompanyIdValue, dialog.AmountValue);
                if (!amountSuccess)
                {
                    MessageBox.Show(FindForm(), $"Could not update the subscription amount:\n{amountMessage}",
                        "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var (planSuccess, planMessage) = await _controller.SetSubscriptionPlanAsync(
                    dialog.CompanyIdValue, dialog.PlanKeyValue);
                if (!planSuccess)
                {
                    MessageBox.Show(FindForm(), $"Could not update the subscription plan:\n{planMessage}",
                        "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                ShowToast("✓ Subscription updated.");
                await LoadData();
            }
            finally
            {
                subscribeBtn.Enabled = true;
            }
        }

        private async void TenantGrid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (tenantGrid.Columns[e.ColumnIndex].Name != "Status") return;

            var row = tenantGrid.Rows[e.RowIndex];
            if (row.Tag is not TenantSubscriptionEntryDto tenant) return;

            string newStatus = row.Cells["Status"].Value?.ToString() ?? "Active";
            var (success, message) = await _controller.SetSubscriptionStatusAsync(tenant.CompanyId, newStatus);
            if (!success)
            {
                MessageBox.Show(FindForm(), $"Could not update the subscription status:\n{message}",
                    "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                await LoadData();
                return;
            }
            ShowToast($"✓ {tenant.CompanyName}'s subscription is now \"{newStatus}\".");
        }

        private static string PlanDisplayName(string key) => key switch
        {
            "Growth" => "Growth",
            "Starter" => "Starter",
            _ => "All Access"
        };

        private void ShowToast(string message)
        {
            toastLabel.Text = message;
            toastLabel.Height = 36;
            toastLabel.Visible = true;
            toastTimer.Stop();
            toastTimer.Start();
        }

        private void HideToast()
        {
            toastLabel.Visible = false;
            toastLabel.Height = 0;
        }
    }
}
