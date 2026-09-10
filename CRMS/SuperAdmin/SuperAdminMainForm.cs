using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS.winforms.Controllers;
using CRMS.winforms.Shared;

namespace CRMS.winforms.SuperAdmin
{
    /// <summary>
    /// Superadmin shell: header + sidebar + content, built the same way as
    /// AdminMainForm/StaffMainForm so every role shares one consistent look.
    /// </summary>
    public partial class SuperAdminMainForm : Form
    {
        private Panel headerPanel = null!;
        private Panel sidebarPanel = null!;
        private Panel contentPanel = null!;

        private NavItem navTenants = null!;
        private NavItem navAnalytics = null!;
        private NavItem navTerms = null!;
        private NavItem navLogout = null!;
        private NavItem? activeNavItem;

        private TenantsControl tenantsControl = null!;
        private AnalyticsReportControl analyticsControl = null!;
        private TermsConditionControl termsControl = null!;

        private readonly SuperAdminController superAdminController;
        private readonly string superAdminName;

        public SuperAdminMainForm(string superAdminName = "Super Admin")
        {
            this.superAdminName = superAdminName;
            superAdminController = new SuperAdminController();
            InitializeComponent();
            SetupHeader();
            SetupSidebar();
            SetupContentPanels();
            NavigatePage("Tenants", navTenants);
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = "CRMS - Superadmin";
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

            sidebarPanel = new Panel { Dock = DockStyle.Left, Width = 240, BackColor = Theme.SidebarBg };

            contentPanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.PageBg };

            Controls.Add(contentPanel);
            Controls.Add(sidebarPanel);
            Controls.Add(headerPanel);

            ResumeLayout(false);
        }

        private void SetupHeader()
        {
            var logoBox = new Panel { Size = new Size(44, 44), Location = new Point(24, 14), BackColor = Theme.Accent };
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
            var avatar = new AvatarCircle(AvatarCircle.GetInitials(superAdminName)) { Size = new Size(40, 40), Location = new Point(0, 0) };

            var nameFont = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            var roleFont = new Font("Segoe UI", 8.5F, FontStyle.Regular);

            var nameLabel = new Label { Text = superAdminName, AutoSize = true, Location = new Point(52, 3), Font = nameFont, ForeColor = Theme.TextPrimary };
            var roleLabel = new Label { Text = "Superadmin", AutoSize = true, Location = new Point(52, 21), Font = roleFont, ForeColor = Theme.TextSecondary };

            int textWidth = Math.Max(
                TextRenderer.MeasureText(superAdminName, nameFont).Width,
                TextRenderer.MeasureText("Superadmin", roleFont).Width);
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
            var brandPanel = new Panel { Dock = DockStyle.Top, Height = 92, BackColor = Theme.SidebarBg };
            brandPanel.Controls.Add(new Label
            {
                Text = "SUPERADMIN",
                AutoSize = true,
                Location = new Point(24, 18),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Theme.SidebarLabel
            });
            brandPanel.Controls.Add(new Label
            {
                Text = "Platform Console",
                AutoSize = true,
                Location = new Point(24, 38),
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Color.White
            });

            var navigationPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 172,
                BackColor = Theme.SidebarBg,
                Padding = new Padding(0, 12, 0, 0)
            };

            navTenants = new NavItem("Tenants & Subscriptions") { Dock = DockStyle.Top, Height = 44 };
            navAnalytics = new NavItem("Analytics Reports") { Dock = DockStyle.Top, Height = 44 };
            navTerms = new NavItem("Terms & Conditions") { Dock = DockStyle.Top, Height = 44 };

            navTenants.Click += (s, e) => NavigatePage("Tenants", navTenants);
            navAnalytics.Click += (s, e) => NavigatePage("Analytics", navAnalytics);
            navTerms.Click += (s, e) => NavigatePage("Terms", navTerms);

            // Docked-top children stack in reverse add order, so add bottom-most first.
            navigationPanel.Controls.Add(navTerms);
            navigationPanel.Controls.Add(navAnalytics);
            navigationPanel.Controls.Add(navTenants);

            var logoutPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 64,
                BackColor = Theme.SidebarBg,
                Padding = new Padding(0, 8, 0, 12)
            };
            navLogout = new NavItem("Logout") { Dock = DockStyle.Fill };
            navLogout.Click += LogoutItem_Click;
            logoutPanel.Controls.Add(navLogout);

            sidebarPanel.Controls.Add(logoutPanel);
            sidebarPanel.Controls.Add(navigationPanel);
            sidebarPanel.Controls.Add(brandPanel);
        }

        private void SetupContentPanels()
        {
            tenantsControl = new TenantsControl(superAdminController) { Dock = DockStyle.Fill, Visible = false };
            analyticsControl = new AnalyticsReportControl(superAdminController) { Dock = DockStyle.Fill, Visible = false };
            termsControl = new TermsConditionControl(superAdminController) { Dock = DockStyle.Fill, Visible = false };

            contentPanel.Controls.Add(tenantsControl);
            contentPanel.Controls.Add(analyticsControl);
            contentPanel.Controls.Add(termsControl);
        }

        private void NavigatePage(string pageName, NavItem selectedNav)
        {
            string selectedPage = superAdminController.NavigateTo(pageName);
            SwitchContent(selectedPage);
            SetActiveNavItem(selectedNav);
        }

        private void SwitchContent(string pageName)
        {
            tenantsControl.Visible = false;
            analyticsControl.Visible = false;
            termsControl.Visible = false;

            switch (pageName)
            {
                case "Tenants":
                    tenantsControl.LoadData();
                    tenantsControl.Visible = true;
                    break;
                case "Analytics":
                    analyticsControl.LoadData();
                    analyticsControl.Visible = true;
                    break;
                case "Terms":
                    termsControl.LoadData();
                    termsControl.Visible = true;
                    break;
            }
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
