using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS.winforms.Controllers;
using CRMS.winforms.Shared;

namespace CRMS.winforms.SuperAdmin
{
    /// <summary>
    /// Superadmin shell: header + sidebar (ModernCrmsLayout, shared with every
    /// other role) + content.
    /// </summary>
    public partial class SuperAdminMainForm : Form
    {
        // Set right before Close() on Logout (never on the window's X button),
        // so Program.cs knows to show the Login screen again instead of exiting.
        public bool LoggedOut { get; private set; }

        private ModernCrmsLayout layout = null!;

        private SuperadminDashboardControl dashboardControl = null!;
        private TenantsControl subscriptionsControl = null!;
        private AnalyticsReportControl reportsControl = null!;
        private TermsConditionControl termsControl = null!;
        private UserManagementControl usersControl = null!;

        private readonly SuperAdminController superAdminController;
        private readonly string superAdminName;

        public SuperAdminMainForm(string superAdminName = "Super Admin", string token = "")
        {
            this.superAdminName = superAdminName;
            superAdminController = new SuperAdminController(token);
            InitializeComponent();
            SetupContentPanels();
            NavigatePage("Dashboard");
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = "CRMS — Platform Console";
            Size = new Size(1480, 940);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1100, 650);
            Icon = IconResources.AppIcon;

            layout = new ModernCrmsLayout(CrmsRole.Superadmin, superAdminName);
            layout.PageRequested += (s, e) => NavigatePage(e.PageKey);
            layout.LogoutRequested += LogoutItem_Click;

            Controls.Add(layout);

            ResumeLayout(false);
        }

        private void SetupContentPanels()
        {
            dashboardControl = new SuperadminDashboardControl(superAdminController) { Dock = DockStyle.Fill, Visible = false };

            subscriptionsControl = new TenantsControl(superAdminController) { Dock = DockStyle.Fill, Visible = false };
            reportsControl = new AnalyticsReportControl(superAdminController) { Dock = DockStyle.Fill, Visible = false };
            termsControl = new TermsConditionControl(superAdminController) { Dock = DockStyle.Fill, Visible = false };
            usersControl = new UserManagementControl(superAdminController) { Dock = DockStyle.Fill, Visible = false };

            layout.ContentPanel.Controls.Add(dashboardControl);
            layout.ContentPanel.Controls.Add(subscriptionsControl);
            layout.ContentPanel.Controls.Add(reportsControl);
            layout.ContentPanel.Controls.Add(termsControl);
            layout.ContentPanel.Controls.Add(usersControl);
        }

        private void NavigatePage(string pageKey)
        {
            string selectedPage = superAdminController.NavigateTo(pageKey);
            SwitchContent(selectedPage);
            layout.SetActivePage(pageKey);
        }

        private void SwitchContent(string pageName)
        {
            dashboardControl.Visible = false;
            subscriptionsControl.Visible = false;
            reportsControl.Visible = false;
            termsControl.Visible = false;
            usersControl.Visible = false;

            switch (pageName)
            {
                case "Dashboard":
                    _ = dashboardControl.LoadData();
                    dashboardControl.Visible = true;
                    break;
                case "Subscriptions":
                    _ = subscriptionsControl.LoadData();
                    subscriptionsControl.Visible = true;
                    break;
                case "Reports":
                    _ = reportsControl.LoadData();
                    reportsControl.Visible = true;
                    break;
                case "Terms":
                    _ = termsControl.LoadData();
                    termsControl.Visible = true;
                    break;
                case "Users":
                    _ = usersControl.LoadData();
                    usersControl.Visible = true;
                    break;
            }
        }

        private void LogoutItem_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(this, "Are you sure you want to log out?", "Logout",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                LoggedOut = true;
                Close();
            }
        }
    }
}
