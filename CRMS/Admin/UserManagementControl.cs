using System;
using System.Drawing;
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
            userGrid.ReadOnly = true;

            userGrid.Columns.Add("Name", "Name");
            userGrid.Columns.Add("Username", "Username");
            userGrid.Columns.Add("Role", "Role");
            userGrid.Columns.Add("Status", "Status");

            userGrid.Columns["Name"].FillWeight = 160;
            userGrid.Columns["Username"].FillWeight = 140;
            userGrid.Columns["Role"].FillWeight = 100;
            userGrid.Columns["Status"].FillWeight = 100;

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

            string fullName = $"{dialog.FirstNameValue} {dialog.MiddleNameValue} {dialog.LastNameValue}"
                .Replace("  ", " ").Trim();

            createAccountBtn.Enabled = false;
            try
            {
                var client = new AuthApiClient();
                var (success, message) = await client.RegisterAsync(
                    _controller.Token, dialog.UsernameValue, dialog.PasswordValue,
                    fullName, dialog.RoleValue, _controller.CompanyId);

                if (!success)
                {
                    MessageBox.Show(FindForm(), $"Could not create the account:\n{message}",
                        "Create Account Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Also reflect it in the local grid — the account list here is
                // still an in-memory display; the account itself now really
                // exists (the call above created it via the API).
                _controller.CreateAccount(
                    dialog.FirstNameValue, dialog.MiddleNameValue, dialog.LastNameValue,
                    dialog.UsernameValue, dialog.RoleValue);

                RefreshAccounts();
            }
            finally
            {
                createAccountBtn.Enabled = true;
            }
        }

        // =========================================================
        // PUBLIC METHODS
        // =========================================================

        /// <summary>Reloads the grid from the controller's account list. Call on every page navigation.</summary>
        public void RefreshAccounts()
        {
            userGrid.Rows.Clear();

            var accounts = _controller.GetAccounts();
            if (accounts.Count == 0)
            {
                ShowEmptyState();
                return;
            }

            foreach (var account in accounts)
            {
                userGrid.Rows.Add(account.FullName, account.Username, account.Role, account.Status);
            }
        }
    }
}
