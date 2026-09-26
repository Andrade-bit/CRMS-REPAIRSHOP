using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS.winforms.Api;
using CRMS.winforms.Controllers;
using CRMS.winforms.Shared;

namespace CRMS.winforms.SuperAdmin
{
    /// <summary>
    /// Superadmin's User Management page: the one Admin account per tenant
    /// across the whole platform, with search plus Reset Password/Suspend
    /// actions. Deliberately no "Create Admin Account" button — tenant
    /// provisioning is a separate, deferred feature.
    /// </summary>
    public partial class UserManagementControl : UserControl
    {
        private readonly SuperAdminController _controller;

        private RoundedTextBox searchBox = null!;
        private DataGridView userGrid = null!;
        private System.Collections.Generic.List<SuperadminAdminAccountEntryDto> _allAccounts = new();

        public UserManagementControl(SuperAdminController controller)
        {
            _controller = controller;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            BackColor = Theme.PageBg;
            Padding = new Padding(Theme.PageMarginX, Theme.PageMarginTop, Theme.PageMarginX, Theme.PageMarginTop);

            searchBox = new RoundedTextBox
            {
                Placeholder = "Search by company, admin name, or username...",
                Size = new Size(320, 38)
            };
            searchBox.ValueChanged += (s, e) => ApplyFilter();

            var searchRow = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = Theme.PageBg };
            searchRow.Controls.Add(searchBox);
            searchRow.Resize += (s, e) => searchBox.Location = new Point(0, 8);
            searchBox.Location = new Point(0, 8);

            var tableCard = Theme.CreateTableCard();

            userGrid = Theme.CreateStyledGrid();
            userGrid.RowTemplate.Height = 52;

            userGrid.Columns.Add("Company", "Company");
            userGrid.Columns.Add("FullName", "Admin");
            userGrid.Columns.Add("Username", "Username");
            userGrid.Columns.Add("Status", "Status");

            var resetColumn = new DataGridViewButtonColumn
            {
                Name = "ResetPassword",
                HeaderText = "",
                Text = "Reset Password",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat
            };
            userGrid.Columns.Add(resetColumn);

            var statusColumn = new DataGridViewButtonColumn
            {
                Name = "ToggleStatus",
                HeaderText = "",
                UseColumnTextForButtonValue = false,
                FlatStyle = FlatStyle.Flat
            };
            userGrid.Columns.Add(statusColumn);

            userGrid.Columns["Company"].FillWeight = 160;
            userGrid.Columns["FullName"].FillWeight = 140;
            userGrid.Columns["Username"].FillWeight = 120;
            userGrid.Columns["Status"].FillWeight = 90;
            userGrid.Columns["ResetPassword"].FillWeight = 140;
            userGrid.Columns["ToggleStatus"].FillWeight = 110;

            foreach (DataGridViewColumn column in userGrid.Columns)
            {
                if (column.Name is "ResetPassword" or "ToggleStatus") continue;
                column.ReadOnly = true;
            }

            userGrid.CellFormatting += (s, e) =>
            {
                if (userGrid.Columns[e.ColumnIndex].Name != "ToggleStatus") return;
                if (userGrid.Rows[e.RowIndex].Tag is not SuperadminAdminAccountEntryDto account) return;

                e.Value = account.Status == "Active" ? "Suspend" : "Reactivate";
                e.FormattingApplied = true;
            };

            userGrid.CellClick += UserGrid_CellClick;

            ShowEmptyState();
            tableCard.Controls.Add(userGrid);

            Controls.Add(tableCard);
            Controls.Add(searchRow);
            Controls.Add(Theme.CreateSpacer());
            Controls.Add(Theme.CreatePageHeader(
                "Company Management", "Admin accounts across every company on the platform", showTitle: false, showSubtitle: false));

            ResumeLayout(false);
        }

        private void ShowEmptyState()
        {
            userGrid.Rows.Add("No admin accounts found.", "", "", "", "", "");
            userGrid.Rows[0].ReadOnly = true;
            userGrid.Rows[0].DefaultCellStyle = new DataGridViewCellStyle
            {
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 9.75F, FontStyle.Italic)
            };
        }

        /// <summary>Reloads the grid from the real API. Call on every page navigation.</summary>
        public async System.Threading.Tasks.Task LoadData()
        {
            _allAccounts = await _controller.GetAdminAccountsAsync();
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            userGrid.CellClick -= UserGrid_CellClick;
            userGrid.Rows.Clear();

            string query = searchBox.Value.Trim();
            var filtered = string.IsNullOrEmpty(query)
                ? _allAccounts
                : _allAccounts.FindAll(a =>
                    a.CompanyName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    a.FullName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    a.Username.Contains(query, StringComparison.OrdinalIgnoreCase));

            if (filtered.Count == 0)
            {
                ShowEmptyState();
            }
            else
            {
                foreach (var account in filtered)
                {
                    int rowIndex = userGrid.Rows.Add(account.CompanyName, account.FullName, account.Username, account.Status, "", "");
                    userGrid.Rows[rowIndex].Tag = account;
                }
            }

            userGrid.CellClick += UserGrid_CellClick;
        }

        private async void UserGrid_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var row = userGrid.Rows[e.RowIndex];
            if (row.Tag is not SuperadminAdminAccountEntryDto account) return;

            string columnName = userGrid.Columns[e.ColumnIndex].Name;

            if (columnName == "ToggleStatus")
            {
                bool suspending = account.Status == "Active";
                string newStatus = suspending ? "Suspended" : "Active";

                var confirm = MessageBox.Show(FindForm(),
                    suspending
                        ? $"Suspend {account.FullName}'s account ({account.CompanyName})? They won't be able to log in until reactivated."
                        : $"Reactivate {account.FullName}'s account ({account.CompanyName})? They'll be able to log in again.",
                    suspending ? "Suspend Account" : "Reactivate Account",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes) return;

                var (success, message) = await _controller.SetAdminAccountStatusAsync(account.Username, newStatus);
                if (!success)
                {
                    MessageBox.Show(FindForm(), $"Could not update the account status:\n{message}",
                        "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                await LoadData();
                return;
            }

            if (columnName != "ResetPassword") return;

            using var dialog = new ChangePasswordDialog(account.Username, account.FullName);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

            var (resetSuccess, resetMessage) = await _controller.ResetAdminPasswordAsync(account.Username, dialog.NewPasswordValue);
            if (!resetSuccess)
            {
                MessageBox.Show(FindForm(), $"Could not update the password:\n{resetMessage}",
                    "Change Password Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MessageBox.Show(FindForm(), $"Password updated for {account.FullName}.",
                "Password Changed", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
