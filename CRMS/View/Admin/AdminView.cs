using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS.winforms.Controllers;
using CRMS.winforms.Shared;

namespace CRMS.Winforms.Admin
{
    /// <summary>
    /// Admin shell: header + sidebar (ModernCrmsLayout, shared with every
    /// other role) + content. Reports has its own expandable sub-menu (Monthly
    /// Sales / Top Customers / Common Repairs / Customer Feedback / Promotion
    /// Effectiveness) that all live inside the same AnalyticReportControl —
    /// picking one just tells it which section to show, it never becomes a
    /// separate page control. Branch Management only shows up once
    /// CheckBranchAvailabilityAsync confirms this tenant actually has one.
    /// </summary>
    public partial class AdminMainForm : Form
    {
        // Set right before Close() on Logout (never on the window's X button),
        // so Program.cs knows to show the Login screen again instead of exiting.
        public bool LoggedOut { get; private set; }

        private readonly string adminName;

        private ModernCrmsLayout layout = null!;

        private DashboardControl dashboardControl = null!;
        private UserManagementControl userManagementControl = null!;
        private CustomerHistoryControl customerHistoryControl = null!;
        private PromotionsReviewControl promotionsControl = null!;
        private AnalyticReportControl reportsControl = null!;
        private BranchManagementControl branchManagementControl = null!;
        private TermsConditionControl termsControl = null!;

        private readonly AdminController adminController;

        public AdminMainForm(string adminName = "Admin", string token = "", int? companyId = null)
        {
            this.adminName = adminName;
            adminController = new AdminController(token, companyId, adminName);
            InitializeComponent();
            SetupContentPanels();
            NavigatePage("Dashboard");
            _ = CheckModuleAccessAsync();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = "CRMS - Admin";
            Size = new Size(1200, 800);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1024, 700);
            Icon = IconResources.AppIcon;

            layout = new ModernCrmsLayout(CrmsRole.Admin, adminName);
            layout.PageRequested += (s, e) => HandlePageRequested(e.PageKey);
            layout.LogoutRequested += LogoutItem_Click;

            // Hidden until CheckBranchAvailabilityAsync confirms this tenant
            // has the Branches table — most tenants won't.
            layout.SetItemVisible("Branches", false);

            var syncIndicator = new SyncStatusIndicator(adminController.GetSyncStatusAsync);
            layout.AddHeaderExtra(syncIndicator);

            Controls.Add(layout);

            ResumeLayout(false);
        }

        /// <summary>Checks both gates that decide what shows in the sidebar:
        /// this tenant's subscription plan (Promotions/Reports/Branches) and,
        /// for Branch Management specifically, whether the tenant's database
        /// even has the Branches table at all — both have to allow it.
        /// Reports also disables the Dashboard's own KPI drill-down popups
        /// when this tenant's plan doesn't include it.</summary>
        private async Task CheckModuleAccessAsync()
        {
            var accessTask = adminController.GetModuleAccessAsync();
            var branchesTask = adminController.GetBranchesAsync();
            await Task.WhenAll(accessTask, branchesTask);

            var access = accessTask.Result;
            bool hasBranchTable = branchesTask.Result != null;

            layout.SetItemVisible("Promotions", access.Promotions);
            layout.SetItemVisible("Reports", access.Reports);
            layout.SetItemVisible("Branches", access.Branches && hasBranchTable);

            dashboardControl.SetDrilldownsEnabled(access.Reports);
        }

        private void SetupContentPanels()
        {
            dashboardControl = new DashboardControl(adminController) { Dock = DockStyle.Fill, Visible = false };
            dashboardControl.ReviewRequested += (s, e) => NavigatePage("Promotions");
            dashboardControl.MonthlySalesRequested += (s, e) => JumpToReportsSection(AnalyticReportControl.SectionSales, "Reports.Sales");
            dashboardControl.CustomerFeedbackRequested += (s, e) => JumpToReportsSection(AnalyticReportControl.SectionFeedback, "Reports.Feedback");
            userManagementControl = new UserManagementControl(adminController) { Dock = DockStyle.Fill, Visible = false };
            customerHistoryControl = new CustomerHistoryControl(allowArchive: true, generatedByName: adminName, generatedByRole: "Admin") { Dock = DockStyle.Fill, Visible = false };
            customerHistoryControl.OnPickupChanged = (ticketId, pickedUp) => adminController.UpdatePickupStatusAsync(ticketId, pickedUp);
            customerHistoryControl.OnFeedbackSubmitted = (ticketId, rating, message) => adminController.SubmitFeedbackAsync(ticketId, rating, message);
            customerHistoryControl.OnPaymentChanged = (ticketId, paymentStatus, amountPaid) => adminController.UpdatePaymentStatusAsync(ticketId, paymentStatus, amountPaid);
            customerHistoryControl.OnArchiveRequested = ticketId => adminController.ArchiveTicketAsync(ticketId);
            promotionsControl = new PromotionsReviewControl(adminController) { Dock = DockStyle.Fill, Visible = false };
            reportsControl = new AnalyticReportControl(adminController) { Dock = DockStyle.Fill, Visible = false };
            branchManagementControl = new BranchManagementControl(adminController) { Dock = DockStyle.Fill, Visible = false };
            termsControl = new TermsConditionControl(adminController) { Dock = DockStyle.Fill, Visible = false };

            layout.ContentPanel.Controls.Add(dashboardControl);
            layout.ContentPanel.Controls.Add(userManagementControl);
            layout.ContentPanel.Controls.Add(customerHistoryControl);
            layout.ContentPanel.Controls.Add(promotionsControl);
            layout.ContentPanel.Controls.Add(reportsControl);
            layout.ContentPanel.Controls.Add(branchManagementControl);
            layout.ContentPanel.Controls.Add(termsControl);
        }

        /// <summary>Reports' five sub-items all arrive here as "Reports.Sales",
        /// "Reports.Customers", etc. — everything else is a normal top-level page.</summary>
        private void HandlePageRequested(string pageKey)
        {
            if (pageKey.StartsWith("Reports.", StringComparison.Ordinal))
            {
                string section = pageKey switch
                {
                    "Reports.Sales" => AnalyticReportControl.SectionSales,
                    "Reports.Customers" => AnalyticReportControl.SectionCustomers,
                    "Reports.Common" => AnalyticReportControl.SectionCommon,
                    "Reports.Feedback" => AnalyticReportControl.SectionFeedback,
                    "Reports.Promotions" => AnalyticReportControl.SectionPromotions,
                    _ => AnalyticReportControl.SectionSales
                };
                reportsControl.ShowOnly(section);
                string selectedReportsPage = adminController.NavigateTo("Reports");
                SwitchContent(selectedReportsPage);
                return;
            }

            NavigatePage(pageKey);
        }

        private void NavigatePage(string pageKey)
        {
            string selectedPage = adminController.NavigateTo(pageKey);
            SwitchContent(selectedPage);
            layout.SetActivePage(pageKey);
        }

        private void SwitchContent(string pageName)
        {
            dashboardControl.Visible = false;
            userManagementControl.Visible = false;
            customerHistoryControl.Visible = false;
            promotionsControl.Visible = false;
            reportsControl.Visible = false;
            branchManagementControl.Visible = false;
            termsControl.Visible = false;

            switch (pageName)
            {
                case "Dashboard":
                    _ = dashboardControl.LoadData();
                    dashboardControl.Visible = true;
                    break;
                case "Users":
                    _ = userManagementControl.RefreshAccounts();
                    userManagementControl.Visible = true;
                    break;
                case "History":
                    _ = LoadHistoryAsync();
                    customerHistoryControl.Visible = true;
                    break;
                case "Promotions":
                    _ = promotionsControl.LoadData();
                    promotionsControl.Visible = true;
                    break;
                case "Reports":
                    reportsControl.Visible = true;
                    break;
                case "Branches":
                    _ = branchManagementControl.LoadData();
                    branchManagementControl.Visible = true;
                    break;
                case "Terms":
                    _ = termsControl.LoadData();
                    termsControl.Visible = true;
                    break;
            }
        }

        /// <summary>Jumps straight to a Reports sub-page from somewhere other
        /// than the sidebar (e.g. clicking the Dashboard's trend/ratings card).
        /// SetActivePage expands the Reports group on its own once it sees the
        /// active key belongs to one of its children.</summary>
        private void JumpToReportsSection(string section, string childPageKey)
        {
            reportsControl.ShowOnly(section);
            string selectedPage = adminController.NavigateTo("Reports");
            SwitchContent(selectedPage);
            layout.SetActivePage(childPageKey);
        }

        private async Task LoadHistoryAsync()
        {
            var history = await adminController.GetTicketHistoryAsync();
            customerHistoryControl.LoadData(history);
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
