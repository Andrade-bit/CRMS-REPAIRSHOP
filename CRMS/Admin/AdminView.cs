using CRMS.api.Admin;
//using CRMS.winforms.Admin;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CRMS.Winforms.Admin
{
    public partial class AdminMainForm : Form
    {
        // =========================================================
        // PANELS
        // =========================================================

        private Panel headerPanel;
        private Panel sidebarPanel;
        private Panel contentPanel;

        // =========================================================
        // NAVIGATION
        // =========================================================

        private NavItem navDashboard;
        private NavItem navUsers;
        private NavItem navMembership;
        private NavItem navReports;
        private NavItem navSettings;
        private NavItem navLogout;
        private NavItem activeNavItem;

        // =========================================================
        // PAGE CONTENT
        // =========================================================

        private DashboardControl dashboardControl;
        private UserManagementControl userManagementControl;
        private MembershipControl membershipControl;
        private AnalyticReportControl reportsControl;
        private Panel settingsContent;

        // =========================================================
        // CONTROLLER
        // =========================================================

        private AdminController adminController;

        // =========================================================
        // ADMIN INFORMATION
        // =========================================================

        private const string AdminName = "Admin Name";
        private const string AdminRole = "Administrator";

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public AdminMainForm()
        {
            InitializeComponent();
            adminController = new AdminController();
            SetupHeader();
            SetupSidebar();
            SetupContentPanels();
            NavigatePage("Dashboard", navDashboard);
        }

        // =========================================================
        // INITIALIZE FORM
        // =========================================================

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = "CRMS - Admin";
            Size = new Size(1200, 800);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.PageBg;
            MinimumSize = new Size(1024, 700);

            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = Theme.PageBg
            };
            headerPanel.Paint += (s, e) =>
            {
                using (var pen = new Pen(Theme.BorderColor, 1))
                {
                    e.Graphics.DrawLine(pen, 0, headerPanel.Height - 1, headerPanel.Width, headerPanel.Height - 1);
                }
            };

            sidebarPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 240,
                BackColor = Theme.SidebarBg
            };

            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.PageBg
            };

            Controls.Add(contentPanel);
            Controls.Add(sidebarPanel);
            Controls.Add(headerPanel);

            ResumeLayout(false);
        }

        // =========================================================
        // HEADER
        // =========================================================

        private void SetupHeader()
        {
            Panel logoBox = new Panel
            {
                Size = new Size(44, 44),
                Location = new Point(24, 14),
                BackColor = Theme.Accent
            };
            Theme.ApplyRoundedRegion(logoBox, 10);

            Label logoText = new Label
            {
                Text = "CRMS",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            logoBox.Controls.Add(logoText);

            // Profile chip: avatar + name/role, pinned to the top-right.
            Panel profilePanel = new Panel
            {
                BackColor = Theme.PageBg,
                Size = new Size(1, 40)
            };

            AvatarCircle avatar = new AvatarCircle(GetInitials(AdminName))
            {
                Size = new Size(40, 40),
                Location = new Point(0, 0)
            };

            Font nameFont = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            Font roleFont = new Font("Segoe UI", 8.5F, FontStyle.Regular);

            Label nameLabel = new Label
            {
                Text = AdminName,
                AutoSize = true,
                Location = new Point(52, 3),
                Font = nameFont,
                ForeColor = Theme.TextPrimary
            };

            Label roleLabel = new Label
            {
                Text = AdminRole,
                AutoSize = true,
                Location = new Point(52, 21),
                Font = roleFont,
                ForeColor = Theme.TextSecondary
            };

            int textWidth = Math.Max(
                TextRenderer.MeasureText(AdminName, nameFont).Width,
                TextRenderer.MeasureText(AdminRole, roleFont).Width);
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

        private static string GetInitials(string name)
        {
            string[] parts = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "?";
            if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();
            return (parts[0][0].ToString() + parts[parts.Length - 1][0]).ToUpperInvariant();
        }

        // =========================================================
        // SIDEBAR
        // =========================================================

        private void SetupSidebar()
        {
            Panel brandPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 92,
                BackColor = Theme.SidebarBg
            };

            Label adminText = new Label
            {
                Text = "ADMIN",
                AutoSize = true,
                Location = new Point(24, 18),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Theme.SidebarLabel
            };

            Label controlPanelText = new Label
            {
                Text = "Control Panel",
                AutoSize = true,
                Location = new Point(24, 38),
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White
            };

            brandPanel.Controls.Add(adminText);
            brandPanel.Controls.Add(controlPanelText);

            Panel navigationPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 280,
                BackColor = Theme.SidebarBg,
                Padding = new Padding(0, 12, 0, 0)
            };

            navDashboard = new NavItem("Dashboard");
            navUsers = new NavItem("Users");
            navMembership = new NavItem("Membership");
            navReports = new NavItem("Reports");
            navSettings = new NavItem("Settings");

            navDashboard.Dock = DockStyle.Top;
            navUsers.Dock = DockStyle.Top;
            navMembership.Dock = DockStyle.Top;
            navReports.Dock = DockStyle.Top;
            navSettings.Dock = DockStyle.Top;

            navDashboard.Height = 44;
            navUsers.Height = 44;
            navMembership.Height = 44;
            navReports.Height = 44;
            navSettings.Height = 44;

            navDashboard.Click += (s, e) => NavigatePage("Dashboard", navDashboard);
            navUsers.Click += (s, e) => NavigatePage("Users", navUsers);
            navMembership.Click += (s, e) => NavigatePage("Membership", navMembership);
            navReports.Click += (s, e) => NavigatePage("Reports", navReports);
            navSettings.Click += (s, e) => NavigatePage("Settings", navSettings);

            // Docked-top children stack in reverse add order, so add bottom-most first.
            navigationPanel.Controls.Add(navSettings);
            navigationPanel.Controls.Add(navReports);
            navigationPanel.Controls.Add(navMembership);
            navigationPanel.Controls.Add(navUsers);
            navigationPanel.Controls.Add(navDashboard);

            Panel logoutPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 64,
                BackColor = Theme.SidebarBg,
                Padding = new Padding(0, 8, 0, 12)
            };

            navLogout = new NavItem("Logout");
            navLogout.Dock = DockStyle.Fill;
            navLogout.Click += LogoutItem_Click;

            logoutPanel.Controls.Add(navLogout);

            sidebarPanel.Controls.Add(logoutPanel);
            sidebarPanel.Controls.Add(navigationPanel);
            sidebarPanel.Controls.Add(brandPanel);
        }

        // =========================================================
        // CONTENT PAGES
        // =========================================================

        private void SetupContentPanels()
        {
            dashboardControl = new DashboardControl { Dock = DockStyle.Fill, Visible = false };
            userManagementControl = new UserManagementControl { Dock = DockStyle.Fill, Visible = false };
            membershipControl = new MembershipControl { Dock = DockStyle.Fill, Visible = false };
            reportsControl = new AnalyticReportControl { Dock = DockStyle.Fill, Visible = false };

            settingsContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.PageBg,
                Padding = new Padding(Theme.PageMarginX, Theme.PageMarginTop, Theme.PageMarginX, Theme.PageMarginTop),
                Visible = false
            };
            settingsContent.Controls.Add(Theme.CreatePlaceholderCard("Settings page is coming soon."));
            settingsContent.Controls.Add(Theme.CreateSpacer());
            settingsContent.Controls.Add(Theme.CreatePageHeader("Settings", "Configure system and account preferences"));

            contentPanel.Controls.Add(dashboardControl);
            contentPanel.Controls.Add(userManagementControl);
            contentPanel.Controls.Add(membershipControl);
            contentPanel.Controls.Add(reportsControl);
            contentPanel.Controls.Add(settingsContent);
        }

        // =========================================================
        // NAVIGATION
        // =========================================================

        private void NavigatePage(string pageName, NavItem selectedNav)
        {
            string selectedPage = adminController.NavigateTo(pageName);
            SwitchContent(selectedPage);
            SetActiveNavItem(selectedNav);
        }

        private void SwitchContent(string pageName)
        {
            dashboardControl.Visible = false;
            userManagementControl.Visible = false;
            membershipControl.Visible = false;
            reportsControl.Visible = false;
            settingsContent.Visible = false;

            switch (pageName)
            {
                case "Dashboard":
                    dashboardControl.Visible = true;
                    break;
                case "Users":
                    userManagementControl.Visible = true;
                    break;
                case "Membership":
                    membershipControl.Visible = true;
                    break;
                case "Reports":
                    reportsControl.Visible = true;
                    break;
                case "Settings":
                    settingsContent.Visible = true;
                    break;
            }
        }

        private void SetActiveNavItem(NavItem selectedItem)
        {
            if (activeNavItem != null && activeNavItem != selectedItem)
            {
                activeNavItem.SetActive(false);
            }
            selectedItem.SetActive(true);
            activeNavItem = selectedItem;
        }

        private void LogoutItem_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "Are you sure you want to log out?",
                "Logout",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                Close();
            }
        }

        // =========================================================
        // AVATAR
        // =========================================================

        private sealed class AvatarCircle : Panel
        {
            private readonly string initials;
            private static readonly Font InitialsFont = new Font("Segoe UI", 9.5F, FontStyle.Bold);

            public AvatarCircle(string initials)
            {
                this.initials = initials;
                DoubleBuffered = true;
                BackColor = Theme.PageBg;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                using (var brush = new SolidBrush(Theme.Accent))
                {
                    e.Graphics.FillEllipse(brush, 0, 0, Width - 1, Height - 1);
                }

                TextRenderer.DrawText(
                    e.Graphics,
                    initials,
                    InitialsFont,
                    new Rectangle(0, 0, Width, Height),
                    Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        // =========================================================
        // NAVIGATION ITEM
        // =========================================================

        private sealed class NavItem : Panel
        {
            private bool active;
            private bool hover;
            private bool pressed;
            private readonly Label textLabel;
            private const int Inset = 14;

            public NavItem(string text)
            {
                DoubleBuffered = true;
                BackColor = Theme.SidebarBg;
                Cursor = Cursors.Hand;
                Padding = new Padding(Inset + 14, 0, Inset, 0);

                textLabel = new Label
                {
                    Text = text,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    ForeColor = Theme.SidebarText,
                    Font = Theme.FontNav,
                    BackColor = Theme.SidebarBg,
                    Cursor = Cursors.Hand
                };

                Controls.Add(textLabel);

                textLabel.Click += (s, e) => base.OnClick(EventArgs.Empty);

                MouseEnter += (s, e) => SetHover(true);
                MouseLeave += (s, e) => SetHover(false);
                textLabel.MouseEnter += (s, e) => SetHover(true);
                textLabel.MouseLeave += (s, e) => SetHover(false);

                MouseDown += (s, e) => { pressed = true; Invalidate(); };
                MouseUp += (s, e) => { pressed = false; Invalidate(); };
                textLabel.MouseDown += (s, e) => { pressed = true; Invalidate(); };
                textLabel.MouseUp += (s, e) => { pressed = false; Invalidate(); };
            }

            private void SetHover(bool value)
            {
                hover = value;
                Color labelBg = active ? Color.White : Theme.SidebarBg;
                textLabel.BackColor = labelBg;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                var bounds = new Rectangle(Inset, 2, Math.Max(0, Width - Inset * 2), Math.Max(0, Height - 4));

                Color? pillColor = null;
                if (active) pillColor = Color.White;
                else if (pressed) pillColor = Theme.SidebarPressed;
                else if (hover) pillColor = Theme.SidebarHover;

                if (pillColor.HasValue && bounds.Width > 0 && bounds.Height > 0)
                {
                    using (var path = Theme.RoundedRect(bounds, 10))
                    using (var brush = new SolidBrush(pillColor.Value))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                }

                base.OnPaint(e);
            }

            public void SetActive(bool isActive)
            {
                active = isActive;

                textLabel.ForeColor = active ? Theme.SidebarActiveText : Theme.SidebarText;
                textLabel.Font = active ? Theme.FontNavActive : Theme.FontNav;
                textLabel.BackColor = active ? Color.White : Theme.SidebarBg;

                Invalidate();
            }
        }
    }
}
