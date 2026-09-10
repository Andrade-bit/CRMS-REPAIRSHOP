using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS.winforms.Controllers;
using CRMS.winforms.Shared;

namespace CRMS.winforms.SuperAdmin
{
    /// <summary>
    /// Superadmin's tenant/subscription management page: every repair shop
    /// onboarded onto the platform, its owning Admin account, and a status
    /// dropdown to activate or suspend its subscription.
    /// </summary>
    public partial class TenantsControl : UserControl
    {
        private readonly SuperAdminController _controller;

        private DataGridView tenantGrid = null!;
        private Label toastLabel = null!;
        private System.Windows.Forms.Timer toastTimer = null!;

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

            var addButton = new Button
            {
                Text = "＋  Add Tenant",
                Width = 150,
                Height = 38,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Accent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            addButton.FlatAppearance.BorderSize = 0;
            addButton.FlatAppearance.MouseOverBackColor = Theme.AccentHover;
            addButton.FlatAppearance.MouseDownBackColor = Theme.AccentPressed;
            Theme.ApplyRoundedRegion(addButton, 8);
            addButton.Click += AddButton_Click;

            var tableCard = Theme.CreateTableCard();

            tenantGrid = Theme.CreateStyledGrid();
            tenantGrid.RowTemplate.Height = 52;

            tenantGrid.Columns.Add("ShopName", "Shop Name");
            tenantGrid.Columns.Add("AdminName", "Admin");
            tenantGrid.Columns.Add("Username", "Username");
            tenantGrid.Columns.Add("Created", "Onboarded");

            var statusColumn = new DataGridViewComboBoxColumn
            {
                Name = "Status",
                HeaderText = "Subscription",
                DataSource = TenantSubscriptionStatus.All,
                FlatStyle = FlatStyle.Flat
            };
            tenantGrid.Columns.Add(statusColumn);

            tenantGrid.Columns["ShopName"].FillWeight = 170;
            tenantGrid.Columns["AdminName"].FillWeight = 150;
            tenantGrid.Columns["Username"].FillWeight = 130;
            tenantGrid.Columns["Created"].FillWeight = 110;
            tenantGrid.Columns["Status"].FillWeight = 110;

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
                "Tenants & Subscriptions", "Repair shops onboarded onto the platform", addButton));

            ResumeLayout(false);
        }

        private void ShowEmptyState()
        {
            // Status cell is left null (not "") since it's a combo-box column —
            // an empty string isn't one of its valid items and would throw.
            tenantGrid.Rows.Add("No tenants onboarded yet.", "", "", "");
            tenantGrid.Rows[0].ReadOnly = true;
            tenantGrid.Rows[0].DefaultCellStyle = new DataGridViewCellStyle
            {
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 9.75F, FontStyle.Italic)
            };
        }

        /// <summary>Reloads the grid from the controller's tenant list. Call on every page navigation.</summary>
        public void LoadData()
        {
            tenantGrid.CellValueChanged -= TenantGrid_CellValueChanged;
            tenantGrid.Rows.Clear();

            var tenants = _controller.GetTenants();

            if (tenants.Count == 0)
            {
                ShowEmptyState();
            }
            else
            {
                foreach (var tenant in tenants)
                {
                    int rowIndex = tenantGrid.Rows.Add(
                        tenant.ShopName,
                        tenant.AdminFullName,
                        tenant.AdminUsername,
                        tenant.CreatedAt.ToString("MMM d, yyyy"),
                        tenant.SubscriptionStatus);

                    tenantGrid.Rows[rowIndex].Tag = tenant.TenantId;
                }
            }

            tenantGrid.CellValueChanged += TenantGrid_CellValueChanged;
        }

        private void TenantGrid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (tenantGrid.Columns[e.ColumnIndex].Name != "Status") return;

            var row = tenantGrid.Rows[e.RowIndex];
            if (row.Tag is not int tenantId) return;

            string newStatus = row.Cells["Status"].Value?.ToString() ?? TenantSubscriptionStatus.Active;
            string shopName = row.Cells["ShopName"].Value?.ToString() ?? "The tenant";

            _controller.UpdateTenantStatus(tenantId, newStatus);
            ShowToast($"✓ {shopName}'s subscription is now \"{newStatus}\".");
        }

        private void AddButton_Click(object? sender, EventArgs e)
        {
            using var dialog = new AddTenantDialog();
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

            // NOTE: dialog.AdminPasswordValue is where a real tenant-provisioning
            // call (creating the Company/CompanyDatabase + Admin Identity account)
            // would use it — not stored here since this in-memory grid only needs
            // the shop name, admin name, and username.
            _controller.CreateTenant(dialog.ShopNameValue, dialog.AdminFullNameValue, dialog.AdminUsernameValue);

            LoadData();
            ShowToast($"✓ {dialog.ShopNameValue} onboarded with Admin \"{dialog.AdminUsernameValue}\".");
        }

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
