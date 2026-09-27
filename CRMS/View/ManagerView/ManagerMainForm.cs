using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS.winforms.Controllers;
using CRMS.winforms.Shared;
using CRMS.winforms.StaffView; // reuses PendingCustomersControl — same ticket work as Staff

namespace CRMS.winforms.ManagerView
{
    /// <summary>
    /// Manager shell: header + sidebar (ModernCrmsLayout, shared with every
    /// other role) + content. Same day-to-day ticket work as Staff (Pending
    /// Customers, Customer History), plus the oversight Admin has (Archive on
    /// History) and drafting Promotions for Admin to review.
    /// </summary>
    public partial class ManagerMainForm : Form
    {
        // Set right before Close() on Logout (never on the window's X button),
        // so Program.cs knows to show the Login screen again instead of exiting.
        public bool LoggedOut { get; private set; }

        private ModernCrmsLayout layout = null!;

        private ManagerDashboardControl dashboardControl = null!;
        private PendingCustomersControl pendingControl = null!;
        private CustomerHistoryControl historyControl = null!;
        private PromotionsComposerControl promotionsControl = null!;

        private readonly ManagerController managerController;
        private readonly string managerName;

        private Label branchPill = null!;

        public ManagerMainForm(string managerName = "Manager Name", string token = "", int? companyId = null)
        {
            this.managerName = managerName;
            managerController = new ManagerController(token, companyId);
            InitializeComponent();
            SetupContentPanels();
            NavigatePage("Dashboard");
            _ = CheckModuleAccessAsync();
            _ = InitBranchPillAsync();
        }

        /// <summary>Shows the branch pill only if this logged-in Manager account is
        /// actually assigned to a branch — most tenants aren't using branches at
        /// all, so the pill just stays hidden for them.</summary>
        private async Task InitBranchPillAsync()
        {
            var profile = await managerController.GetMyProfileAsync();
            if (string.IsNullOrWhiteSpace(profile.BranchName)) return;

            branchPill.Text = $"\U0001F3E2 {profile.BranchName}";
            branchPill.Visible = true;
            Theme.ApplyRoundedRegion(branchPill, branchPill.Height / 2);
        }

        /// <summary>Hides Promotions if this tenant's subscription plan
        /// doesn't include it.</summary>
        private async Task CheckModuleAccessAsync()
        {
            var access = await managerController.GetModuleAccessAsync();
            layout.SetItemVisible("Promotions", access.Promotions);
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = "CRMS - Manager";
            Size = new Size(1200, 800);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1024, 700);
            Icon = IconResources.AppIcon;

            layout = new ModernCrmsLayout(CrmsRole.Manager, managerName);
            layout.PageRequested += (s, e) => NavigatePage(e.PageKey);
            layout.LogoutRequested += LogoutItem_Click;

            branchPill = new Label
            {
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                BackColor = Color.FromArgb(230, 241, 251),
                ForeColor = Color.FromArgb(24, 95, 165),
                AutoSize = true,
                Padding = new Padding(10, 5, 10, 5),
                Visible = false
            };
            layout.AddHeaderExtra(branchPill);

            var syncIndicator = new SyncStatusIndicator(managerController.GetSyncStatusAsync);
            layout.AddHeaderExtra(syncIndicator);

            Controls.Add(layout);

            ResumeLayout(false);
        }

        private void SetupContentPanels()
        {
            dashboardControl = new ManagerDashboardControl(managerController) { Dock = DockStyle.Fill, Visible = false };
            dashboardControl.PromotionsRequested += (s, e) => NavigatePage("Promotions");

            pendingControl = new PendingCustomersControl(managerController) { Dock = DockStyle.Fill, Visible = false };

            historyControl = new CustomerHistoryControl(allowArchive: true, generatedByName: managerName, generatedByRole: "Manager") { Dock = DockStyle.Fill, Visible = false };
            historyControl.OnPickupChanged = (ticketId, pickedUp) => managerController.UpdatePickupStatusAsync(ticketId, pickedUp);
            historyControl.OnFeedbackSubmitted = (ticketId, rating, message) => managerController.SubmitFeedbackAsync(ticketId, rating, message);
            historyControl.OnPaymentChanged = (ticketId, paymentStatus, amountPaid) => managerController.UpdatePaymentStatusAsync(ticketId, paymentStatus, amountPaid);
            historyControl.OnArchiveRequested = ticketId => managerController.ArchiveTicketAsync(ticketId);

            promotionsControl = new PromotionsComposerControl(managerController) { Dock = DockStyle.Fill, Visible = false };

            layout.ContentPanel.Controls.Add(dashboardControl);
            layout.ContentPanel.Controls.Add(pendingControl);
            layout.ContentPanel.Controls.Add(historyControl);
            layout.ContentPanel.Controls.Add(promotionsControl);
        }

        private void NavigatePage(string pageKey)
        {
            string selectedPage = managerController.NavigateTo(pageKey);
            SwitchContent(selectedPage);
            layout.SetActivePage(pageKey);
        }

        private void SwitchContent(string pageName)
        {
            dashboardControl.Visible = false;
            pendingControl.Visible = false;
            historyControl.Visible = false;
            promotionsControl.Visible = false;

            switch (pageName)
            {
                case "Dashboard":
                    _ = dashboardControl.LoadData();
                    dashboardControl.Visible = true;
                    break;
                case "Pending":
                    _ = pendingControl.LoadData();
                    pendingControl.Visible = true;
                    break;
                case "History":
                    _ = LoadHistoryAsync();
                    historyControl.Visible = true;
                    break;
                case "Promotions":
                    _ = promotionsControl.LoadData();
                    promotionsControl.Visible = true;
                    break;
            }
        }

        private async Task LoadHistoryAsync()
        {
            var history = await managerController.GetTicketHistoryAsync();
            historyControl.LoadData(history);
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
