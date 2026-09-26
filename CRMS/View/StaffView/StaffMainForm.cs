using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
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

        public StaffMainForm(string staffName = "Staff Name", string token = "", int? companyId = null)
        {
            this.staffName = staffName;
            staffController = new StaffController(token, companyId);
            InitializeComponent();
            SetupContentPanels();
            NavigatePage("Pending");
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = "CRMS - Staff";
            Size = new Size(1200, 800);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1024, 700);

            layout = new ModernCrmsLayout(CrmsRole.Staff, staffName);
            layout.PageRequested += (s, e) => NavigatePage(e.PageKey);
            layout.LogoutRequested += LogoutItem_Click;

            var syncIndicator = new SyncStatusIndicator(staffController.GetSyncStatusAsync);
            layout.Header.Controls.Add(syncIndicator);

            void RepositionSync()
            {
                syncIndicator.Location = new Point(
                    layout.Header.Width - ModernCrmsLayout.ProfileWidth - layout.Header.Padding.Right - syncIndicator.Width - 24,
                    (layout.Header.Height - syncIndicator.Height) / 2);
            }
            layout.Header.Resize += (s, e) => RepositionSync();
            RepositionSync();

            Controls.Add(layout);

            ResumeLayout(false);
        }

        private void SetupContentPanels()
        {
            pendingControl = new PendingCustomersControl(staffController) { Dock = DockStyle.Fill, Visible = false };
            historyControl = new CustomerHistoryControl { Dock = DockStyle.Fill, Visible = false };
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
