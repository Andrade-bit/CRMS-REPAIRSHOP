using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS.winforms.Api;
using CRMS.winforms.Controllers;
using CRMS.winforms.Shared;

namespace CRMS.Winforms.Admin
{
    public partial class UserManagementControl : UserControl
    {
        private readonly AdminController _controller;

        private DataGridView userGrid;
        private Panel tableCard;
        private Button createAccountBtn;

        public UserManagementControl(AdminController controller)
        {
            _controller = controller;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.BackColor = Theme.PageBg;
            this.Padding = new Padding(Theme.PageMarginX, Theme.PageMarginTop, Theme.PageMarginX, Theme.PageMarginTop);

            // ============================================
            // ACTION BUTTON (goes into the shared page header)
            // ============================================
            createAccountBtn = new Button
            {
                Text = "＋  Create Account",
                Width = 168,
                Height = 38,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Accent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            createAccountBtn.FlatAppearance.BorderSize = 0;
            createAccountBtn.FlatAppearance.MouseOverBackColor = Theme.AccentHover;
            createAccountBtn.FlatAppearance.MouseDownBackColor = Theme.AccentPressed;
            Theme.ApplyRoundedRegion(createAccountBtn, 8);
            createAccountBtn.Click += CreateAccountBtn_Click;

            // ============================================
            // TABLE CARD (subtle bordered container)
            // ============================================
            tableCard = Theme.CreateTableCard();

            userGrid = Theme.CreateStyledGrid();
            userGrid.RowTemplate.Height = 52;

            userGrid.Columns.Add("Name", "Name");
            userGrid.Columns.Add("Username", "Username");
            userGrid.Columns.Add("Role", "Role");
            userGrid.Columns.Add("Status", "Status");

            var editColumn = new DataGridViewButtonColumn
            {
                Name = "Edit",
                HeaderText = "",
                Text = "\U0001F511 Change Password",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat
            };
            userGrid.Columns.Add(editColumn);

            var statusColumn = new DataGridViewButtonColumn
            {
                Name = "ToggleStatus",
                HeaderText = "",
                UseColumnTextForButtonValue = false, // per-row text set in CellFormatting below
                FlatStyle = FlatStyle.Flat
            };
            userGrid.Columns.Add(statusColumn);

            userGrid.Columns["Name"].FillWeight = 140;
            userGrid.Columns["Username"].FillWeight = 120;
            userGrid.Columns["Role"].FillWeight = 80;
            userGrid.Columns["Status"].FillWeight = 80;
            userGrid.Columns["Edit"].FillWeight = 140;
            userGrid.Columns["ToggleStatus"].FillWeight = 110;

            foreach (DataGridViewColumn column in userGrid.Columns)
            {
                if (column.Name == "Edit" || column.Name == "ToggleStatus") continue;
                column.ReadOnly = true;
            }

            // The button's own label switches per row: "Suspend" for an Active
            // account, "Reactivate" for a Suspended one — Superadmin/Admin rows
            // (not Staff/Manager) get no button at all, since only Staff/Manager
            // can be suspended by an Admin.
            userGrid.CellFormatting += (s, e) =>
            {
                if (userGrid.Columns[e.ColumnIndex].Name != "ToggleStatus") return;
                if (userGrid.Rows[e.RowIndex].Tag is not UserAccount account) return;

                bool canToggle = account.Role is "Staff" or "Manager";
                e.Value = canToggle
                    ? (account.Status == "Active" ? "⏸ Suspend" : "▶ Reactivate")
                    : "";
                e.FormattingApplied = true;
            };

            userGrid.CellClick += UserGrid_CellClick;
            userGrid.CellPainting += UserGrid_CellPainting;

            ShowEmptyState();

            tableCard.Controls.Add(userGrid);

            // ============================================
            // ADD CONTROLS
            // ============================================
            this.Controls.Add(tableCard);
            this.Controls.Add(Theme.CreateSpacer());
            this.Controls.Add(Theme.CreatePageHeader(
                "User Management", "Manage user accounts, roles, and access", createAccountBtn, showTitle: false, showSubtitle: false));

            this.ResumeLayout(false);
        }

        private void ShowEmptyState()
        {
            userGrid.Rows.Add("No users found.", "", "", "");
            userGrid.Rows[0].ReadOnly = true;
            userGrid.Rows[0].DefaultCellStyle = new DataGridViewCellStyle
            {
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 9.75F, FontStyle.Italic),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0)
            };
        }

        private async void CreateAccountBtn_Click(object? sender, EventArgs e)
        {
            var branches = await _controller.GetBranchesAsync();

            using var dialog = new CreateAccountDialog(branches);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

            createAccountBtn.Enabled = false;
            try
            {
                var (success, message) = await _controller.CreateAccountAsync(
                    dialog.FirstNameValue, dialog.MiddleNameValue, dialog.LastNameValue,
                    dialog.UsernameValue, dialog.PasswordValue, dialog.RoleValue, dialog.BranchIdValue);

                if (!success)
                {
                    MessageBox.Show(FindForm(), $"Could not create the account:\n{message}",
                        "Create Account Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                await RefreshAccounts();
            }
            finally
            {
                createAccountBtn.Enabled = true;
            }
        }

        /// <summary>Draws the padlock icon next to "Suspend" — "Reactivate" stays
        /// plain text, since locking/unlocking is really only one clear icon
        /// (there's no obvious "unlock" counterpart to pair with it here).</summary>
        private void UserGrid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (userGrid.Columns[e.ColumnIndex].Name != "ToggleStatus") return;

            string label = e.Value?.ToString() ?? string.Empty;
            e.PaintBackground(e.CellBounds, true);

            if (label.Length == 0)
            {
                e.Handled = true;
                return;
            }

            if (label.Contains("Suspend"))
            {
                const int iconSize = 16;
                const int gap = 6;
                var textSize = TextRenderer.MeasureText("Suspend", userGrid.Font);
                int totalWidth = iconSize + gap + textSize.Width;
                int startX = e.CellBounds.X + (e.CellBounds.Width - totalWidth) / 2;
                int centerY = e.CellBounds.Y + e.CellBounds.Height / 2;

                var iconRect = new Rectangle(startX, centerY - iconSize / 2, iconSize, iconSize);
                e.Graphics!.DrawImage(IconResources.Get("lock"), iconRect);

                var textRect = new Rectangle(startX + iconSize + gap, e.CellBounds.Y, textSize.Width + 6, e.CellBounds.Height);
                TextRenderer.DrawText(e.Graphics, "Suspend", userGrid.Font, textRect, Theme.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
            else
            {
                TextRenderer.DrawText(e.Graphics!, label, userGrid.Font, e.CellBounds, Theme.TextPrimary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }

            e.Handled = true;
        }

        private async void UserGrid_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var row = userGrid.Rows[e.RowIndex];
            if (row.Tag is not UserAccount account) return;

            string columnName = userGrid.Columns[e.ColumnIndex].Name;

            if (columnName == "ToggleStatus")
            {
                if (account.Role is not ("Staff" or "Manager")) return;

                bool suspending = account.Status == "Active";
                string newStatus = suspending ? "Suspended" : "Active";

                var confirm = MessageBox.Show(FindForm(),
                    suspending
                        ? $"Suspend {account.FullName}'s account? They won't be able to log in until reactivated."
                        : $"Reactivate {account.FullName}'s account? They'll be able to log in again.",
                    suspending ? "Suspend Account" : "Reactivate Account",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes) return;

                var (statusSuccess, statusMessage) = await _controller.SetAccountStatusAsync(account.Username, newStatus);
                if (!statusSuccess)
                {
                    MessageBox.Show(FindForm(), $"Could not update the account status:\n{statusMessage}",
                        "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                await RefreshAccounts();
                return;
            }

            if (columnName != "Edit") return;

            using var dialog = new ChangePasswordDialog(account.Username, account.FullName);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

            var (success, message) = await _controller.ResetPasswordAsync(account.Username, dialog.NewPasswordValue);

            if (!success)
            {
                MessageBox.Show(FindForm(), $"Could not update the password:\n{message}",
                    "Change Password Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MessageBox.Show(FindForm(), $"Password updated for {account.FullName}.",
                "Password Changed", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // =========================================================
        // PUBLIC METHODS
        // =========================================================

        /// <summary>Reloads the grid with real accounts for this Admin's tenant. Call on every page navigation.</summary>
        public async Task RefreshAccounts()
        {
            userGrid.CellClick -= UserGrid_CellClick;
            userGrid.Rows.Clear();

            var accounts = await _controller.GetAccountsAsync();

            if (accounts.Count == 0)
            {
                ShowEmptyState();
            }
            else
            {
                foreach (var account in accounts)
                {
                    int rowIndex = userGrid.Rows.Add(account.FullName, account.Username, account.Role, account.Status);
                    userGrid.Rows[rowIndex].Tag = account;
                }
            }

            userGrid.CellClick += UserGrid_CellClick;
        }
    }
}
