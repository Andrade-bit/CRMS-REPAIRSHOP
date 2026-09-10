using System;
using System.Drawing;
using System.Windows.Forms;

namespace CRMS.Winforms.Admin
{
    public partial class UserManagementControl : UserControl
    {
        private DataGridView userGrid;
        private Panel tableCard;
        private Button createAccountBtn;

        public UserManagementControl()
        {
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
            createAccountBtn.Click += (s, e) =>
            {
                MessageBox.Show("Open Create Account form here.", "Create Account",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            // ============================================
            // TABLE CARD (subtle bordered container)
            // ============================================
            tableCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1)
            };
            tableCard.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(Theme.BorderColor, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, tableCard.Width - 1, tableCard.Height - 1);
                }
            };

            userGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Theme.BorderColor,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = false,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 48 },
                ColumnHeadersHeight = 44,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                EnableHeadersVisualStyles = false,
                Font = new Font("Segoe UI", 9.75F),
                ForeColor = Theme.TextPrimary
            };

            userGrid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Theme.HeaderBg,
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 20, 0)
            };

            userGrid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 9.75F),
                Padding = new Padding(20, 0, 20, 0),
                SelectionBackColor = Theme.SelectionTint,
                SelectionForeColor = Theme.TextPrimary
            };

            userGrid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Theme.RowAlt,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 9.75F),
                Padding = new Padding(20, 0, 20, 0),
                SelectionBackColor = Theme.SelectionTint,
                SelectionForeColor = Theme.TextPrimary
            };

            userGrid.Columns.Add("Name", "Name");
            userGrid.Columns.Add("Role", "Role");
            userGrid.Columns.Add("Status", "Status");

            userGrid.Columns["Name"].FillWeight = 160;
            userGrid.Columns["Role"].FillWeight = 120;
            userGrid.Columns["Status"].FillWeight = 120;

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
            userGrid.Rows.Add("No users found.", "", "");
            userGrid.Rows[0].DefaultCellStyle = new DataGridViewCellStyle
            {
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 9.75F, FontStyle.Italic),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0)
            };
        }

        // =========================================================
        // PUBLIC METHODS
        // =========================================================

        public void AddUser(string name, string role, string status)
        {
            if (userGrid.Rows.Count == 1 && userGrid.Rows[0].Cells[0].Value?.ToString() == "No users found.")
            {
                userGrid.Rows.Clear();
            }

            userGrid.Rows.Add(name, role, status);
        }

        public void ClearUsers()
        {
            userGrid.Rows.Clear();
            ShowEmptyState();
        }
    }
}
