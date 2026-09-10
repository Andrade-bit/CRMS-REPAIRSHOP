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
                Text = "Change Password",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat
            };
            userGrid.Columns.Add(editColumn);

            userGrid.Columns["Name"].FillWeight = 150;
            userGrid.Columns["Username"].FillWeight = 130;
            userGrid.Columns["Role"].FillWeight = 90;
            userGrid.Columns["Status"].FillWeight = 90;
            userGrid.Columns["Edit"].FillWeight = 150;

            foreach (DataGridViewColumn column in userGrid.Columns)
            {
                if (column.Name == "Edit") continue;
                column.ReadOnly = true;
            }

            userGrid.CellClick += UserGrid_CellClick;

            ShowEmptyState();

            tableCard.Controls.Add(userGrid);

            // ============================================
            // ADD CONTROLS
            // ============================================
            this.Controls.Add(tableCard);
            this.Controls.Add(Theme.CreateSpacer());
            this.Controls.Add(Theme.CreatePageHeader(
                "User Management", "Manage user accounts, roles, and access", createAccountBtn));

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
            using var dialog = new CreateAccountDialog();
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

            createAccountBtn.Enabled = false;
            try
            {
                var (success, message) = await _controller.CreateAccountAsync(
                    dialog.FirstNameValue, dialog.MiddleNameValue, dialog.LastNameValue,
                    dialog.UsernameValue, dialog.PasswordValue, dialog.RoleValue);

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

        private async void UserGrid_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (userGrid.Columns[e.ColumnIndex].Name != "Edit") return;

            var row = userGrid.Rows[e.RowIndex];
            if (row.Tag is not UserAccount account) return;

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
