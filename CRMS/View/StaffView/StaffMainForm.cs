using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS.winforms; // reuses RoundedTextBox from Login.cs
using CRMS.winforms.Controllers;
using CRMS.winforms.Shared;

namespace CRMS.winforms.StaffView
{
    /// <summary>
    /// Staff shell: header + sidebar (ModernCrmsLayout, shared with every
    /// other role) + content. Only this MainForm's own page-navigation logic
    /// lives here — the shell visuals themselves come from the shared component.
    /// </summary>
    public partial class StaffMainForm : Form
    {
        // Set right before Close() on Logout (never on the window's X button),
        // so Program.cs knows to show the Login screen again instead of exiting.
        public bool LoggedOut { get; private set; }

        private ModernCrmsLayout layout = null!;

        private PendingCustomersControl pendingControl = null!;
        private CustomerHistoryControl historyControl = null!;

        private readonly StaffController staffController;
        private readonly string staffName;

        private SyncStatusIndicator syncIndicator = null!;
        private Label branchPill = null!;
        private RoundedTextBox txtFindTicket = null!;

        public StaffMainForm(string staffName = "Staff Name", string token = "", int? companyId = null)
        {
            this.staffName = staffName;
            staffController = new StaffController(token, companyId);
            InitializeComponent();
            SetupContentPanels();
            NavigatePage("Pending");
            _ = InitBranchPillAsync();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = "CRMS - Staff";
            Size = new Size(1200, 800);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1024, 700);
            Icon = IconResources.AppIcon;

            layout = new ModernCrmsLayout(CrmsRole.Staff, staffName);
            layout.PageRequested += (s, e) => NavigatePage(e.PageKey);
            layout.LogoutRequested += LogoutItem_Click;

            // Added left-to-right, so on screen they read (nearest profile
            // first): find-ticket, branch pill, sync status, profile.
            // AddHeaderExtra is Dock-managed — no manual Header.Width math,
            // no Resize plumbing, correctly placed on every real layout pass.
            txtFindTicket = new RoundedTextBox
            {
                Size = new Size(160, 34),
                Placeholder = "Find ticket #...",
                TextSize = 8.75F
            };
            txtFindTicket.ValueKeyDown += async (s, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.SuppressKeyPress = true;
                await RunTicketLookupAsync();
            };
            layout.AddHeaderExtra(txtFindTicket);

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

            syncIndicator = new SyncStatusIndicator(staffController.GetSyncStatusAsync);
            layout.AddHeaderExtra(syncIndicator);

            Controls.Add(layout);

            ResumeLayout(false);
        }

        /// <summary>Shows the branch pill only if this logged-in Staff account is
        /// actually assigned to a branch — most tenants aren't using branches at
        /// all, so the pill just stays hidden for them.</summary>
        private async Task InitBranchPillAsync()
        {
            var profile = await staffController.GetMyProfileAsync();
            if (string.IsNullOrWhiteSpace(profile.BranchName)) return;

            branchPill.Text = $"\U0001F3E2 {profile.BranchName}";
            branchPill.Visible = true;
            Theme.ApplyRoundedRegion(branchPill, branchPill.Height / 2);
        }

        /// <summary>Parses whatever digits are in the find-ticket box (accepts
        /// "142", "0142", or "#0142" alike) and shows the result in a small popup.</summary>
        private async Task RunTicketLookupAsync()
        {
            string digits = new string(txtFindTicket.Value.Where(char.IsDigit).ToArray());
            if (digits.Length == 0 || !int.TryParse(digits, out int ticketId)) return;

            var result = await staffController.LookupTicketAsync(ticketId);
            txtFindTicket.Value = "";

            using var dialog = new TicketLookupDialog(ticketId, result);
            dialog.ShowDialog(this);
        }

        private void SetupContentPanels()
        {
            pendingControl = new PendingCustomersControl(staffController) { Dock = DockStyle.Fill, Visible = false };
            historyControl = new CustomerHistoryControl(generatedByName: staffName, generatedByRole: "Staff") { Dock = DockStyle.Fill, Visible = false };
            historyControl.OnPickupChanged = (ticketId, pickedUp) => staffController.UpdatePickupStatusAsync(ticketId, pickedUp);
            historyControl.OnFeedbackSubmitted = (ticketId, rating, message) => staffController.SubmitFeedbackAsync(ticketId, rating, message);
            historyControl.OnPaymentChanged = (ticketId, paymentStatus, amountPaid) => staffController.UpdatePaymentStatusAsync(ticketId, paymentStatus, amountPaid);

            layout.ContentPanel.Controls.Add(pendingControl);
            layout.ContentPanel.Controls.Add(historyControl);
        }

        private void NavigatePage(string pageKey)
        {
            string selectedPage = staffController.NavigateTo(pageKey);
            SwitchContent(selectedPage);
            layout.SetActivePage(pageKey);
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
