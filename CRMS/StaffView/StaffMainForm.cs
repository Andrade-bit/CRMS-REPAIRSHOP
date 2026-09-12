using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS.winforms.Controllers;
using CRMS.winforms.Shared;
using LogoMark = CRMS.winforms.LogoMark;

namespace CRMS.winforms.StaffView
{
    /// <summary>
    /// Staff shell: header + sidebar + content. Visually distinct from the
    /// other roles' shells (a blue brand gradient sidebar matching the logo/
    /// login palette) via local overrides only — the shared Theme, NavItem and
    /// AvatarCircle classes all default to their original look, so Admin,
    /// Manager and Superadmin are unaffected by this file.
    /// </summary>
    public partial class StaffMainForm : Form
    {
        // Staff-only accent palette (matches the logo and the redesigned login
        // screen). Kept local to this file rather than in Theme.cs so Admin/
        // Manager/Superadmin keep their existing look.
        private static readonly Color StaffAccent = Color.FromArgb(14, 165, 233);
        private static readonly Color StaffAccentHover = Color.FromArgb(10, 116, 163);

        private Panel headerPanel = null!;
        private Panel sidebarPanel = null!;
        private Panel contentPanel = null!;

        private NavItem navPending = null!;
        private NavItem navHistory = null!;
        private NavItem navLogout = null!;
        private NavItem? activeNavItem;

        private PendingCustomersControl pendingControl = null!;
        private CustomerHistoryControl historyControl = null!;

        private readonly StaffController staffController;
        private readonly string staffName;

        public StaffMainForm(string staffName = "Staff Name", string token = "", int? companyId = null)
        {
            this.staffName = staffName;
            staffController = new StaffController(token, companyId);
            InitializeComponent();
            SetupHeader();
            SetupSidebar();
            SetupContentPanels();
            NavigatePage("Pending", navPending);
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = "CRMS - Staff";
            Size = new Size(1200, 800);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.PageBg;
            MinimumSize = new Size(1024, 700);

            headerPanel = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Theme.PageBg };
            headerPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.BorderColor, 1);
                e.Graphics.DrawLine(pen, 0, headerPanel.Height - 1, headerPanel.Width, headerPanel.Height - 1);
            };

            sidebarPanel = new Panel { Dock = DockStyle.Left, Width = 240, BackColor = StaffAccent };

            contentPanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.PageBg };

            Controls.Add(contentPanel);
            Controls.Add(sidebarPanel);
            Controls.Add(headerPanel);

            ResumeLayout(false);
        }

        private void SetupHeader()
        {
            var logoBox = new Panel { Size = new Size(44, 44), Location = new Point(24, 14), BackColor = StaffAccent };
            Theme.ApplyRoundedRegion(logoBox, 10);
            logoBox.Controls.Add(new Label
            {
                Text = "CRMS",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            });

            var profilePanel = new Panel { BackColor = Theme.PageBg, Size = new Size(1, 40) };
            var avatar = new AvatarCircle(AvatarCircle.GetInitials(staffName)) { Size = new Size(40, 40), Location = new Point(0, 0), AccentColor = StaffAccent };

            var nameFont = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            var roleFont = new Font("Segoe UI", 8.5F, FontStyle.Regular);

            var nameLabel = new Label { Text = staffName, AutoSize = true, Location = new Point(52, 3), Font = nameFont, ForeColor = Theme.TextPrimary };
            var roleLabel = new Label { Text = "Staff", AutoSize = true, Location = new Point(52, 21), Font = roleFont, ForeColor = Theme.TextSecondary };

            int textWidth = Math.Max(
                TextRenderer.MeasureText(staffName, nameFont).Width,
                TextRenderer.MeasureText("Staff", roleFont).Width);
            profilePanel.Width = 52 + textWidth;

            profilePanel.Controls.Add(nameLabel);
            profilePanel.Controls.Add(roleLabel);
            profilePanel.Controls.Add(avatar);

            void RepositionProfile()
            {
                profilePanel.Location = new Point(
                    headerPanel.Width - profilePanel.Width - 24,
                    (headerPanel.Height - profilePanel.Height) / 2);
            }
            headerPanel.Resize += (s, e) => RepositionProfile();

            headerPanel.Controls.Add(logoBox);
            headerPanel.Controls.Add(profilePanel);
            RepositionProfile();
        }

        private void SetupSidebar()
        {
            var brandPanel = new Panel { Dock = DockStyle.Top, Height = 148, BackColor = StaffAccent };

            var logo = new LogoMark
            {
                Size = new Size(64, 50),
                Location = new Point((sidebarPanel.Width - 64) / 2, 16),
                Anchor = AnchorStyles.Top,
                AccentColor = StaffAccent,
                BaseColor = StaffAccentHover,
                ScreenColor = Color.White
            };
            brandPanel.Controls.Add(logo);
            brandPanel.Resize += (s, e) => logo.Location = new Point((brandPanel.Width - logo.Width) / 2, 16);

            var staffLabel = new Label
            {
                Text = "STAFF",
                AutoSize = true,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(196, 230, 250),
                BackColor = Color.Transparent
            };
            var deskLabel = new Label
            {
                Text = "Repair Desk",
                AutoSize = true,
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
            brandPanel.Controls.Add(staffLabel);
            brandPanel.Controls.Add(deskLabel);
            void CenterBrandLabels()
            {
                staffLabel.Location = new Point((brandPanel.Width - staffLabel.PreferredWidth) / 2, 78);
                deskLabel.Location = new Point((brandPanel.Width - deskLabel.PreferredWidth) / 2, 96);
            }
            brandPanel.Resize += (s, e) => CenterBrandLabels();
            CenterBrandLabels();

            var navigationPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 128,
                BackColor = StaffAccent,
                Padding = new Padding(0, 12, 0, 0)
            };

            navPending = new NavItem("Pending Customers") { Dock = DockStyle.Top, Height = 44, TransparentMode = true };
            navHistory = new NavItem("Customer History") { Dock = DockStyle.Top, Height = 44, TransparentMode = true };

            navPending.Click += (s, e) => NavigatePage("Pending", navPending);
            navHistory.Click += (s, e) => NavigatePage("History", navHistory);

            // Docked-top children stack in reverse add order, so add bottom-most first.
            navigationPanel.Controls.Add(navHistory);
            navigationPanel.Controls.Add(navPending);

            var logoutPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 64,
                BackColor = StaffAccent,
                Padding = new Padding(0, 8, 0, 12)
            };
            navLogout = new NavItem("Logout") { Dock = DockStyle.Fill, TransparentMode = true };
            navLogout.Click += LogoutItem_Click;
            logoutPanel.Controls.Add(navLogout);

            sidebarPanel.Controls.Add(logoutPanel);
            sidebarPanel.Controls.Add(navigationPanel);
            sidebarPanel.Controls.Add(brandPanel);
        }

        private void SetupContentPanels()
        {
            pendingControl = new PendingCustomersControl(staffController) { Dock = DockStyle.Fill, Visible = false };
            historyControl = new CustomerHistoryControl { Dock = DockStyle.Fill, Visible = false };
            historyControl.OnPickupChanged = (ticketId, pickedUp) => staffController.UpdatePickupStatusAsync(ticketId, pickedUp);

            contentPanel.Controls.Add(pendingControl);
            contentPanel.Controls.Add(historyControl);
        }

        private void NavigatePage(string pageName, NavItem selectedNav)
        {
            string selectedPage = staffController.NavigateTo(pageName);
            SwitchContent(selectedPage);
            SetActiveNavItem(selectedNav);
        }

        private void SwitchContent(string pageName)
        {
            pendingControl.Visible = false;
            historyControl.Visible = false;

            switch (pageName)
            {
                case "Pending":
                    _ = pendingControl.LoadData();
                    pendingControl.Visible = true;
                    break;
                case "History":
                    _ = LoadHistoryAsync();
                    historyControl.Visible = true;
                    break;
            }
        }

        private async Task LoadHistoryAsync()
        {
            var history = await staffController.GetTicketHistoryAsync();
            historyControl.LoadData(history);
        }

        private void SetActiveNavItem(NavItem selectedItem)
        {
            if (activeNavItem != null && activeNavItem != selectedItem)
                activeNavItem.SetActive(false);

            selectedItem.SetActive(true);
            activeNavItem = selectedItem;
        }

        private void LogoutItem_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(this, "Are you sure you want to log out?", "Logout",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
                Close();
        }
    }
}
