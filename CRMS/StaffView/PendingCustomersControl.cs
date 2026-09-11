using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS.winforms.Controllers;
using CRMS.winforms.Shared;

namespace CRMS.winforms.StaffView
{
    /// <summary>
    /// Staff's main working page: every walk-in device that hasn't been marked
    /// "Repaired" yet, with a status dropdown per row (changing it notifies the
    /// customer by email/SMS) and a "+ Add Customer" popup for new intakes.
    /// Backed by the real per-tenant database via CRMS.api's /api/tickets.
    /// </summary>
    public partial class PendingCustomersControl : UserControl
    {
        private readonly StaffController _controller;

        private DataGridView pendingGrid = null!;
        private Label toastLabel = null!;
        private System.Windows.Forms.Timer toastTimer = null!;

        public PendingCustomersControl(StaffController controller)
        {
            _controller = controller;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            BackColor = Theme.PageBg;
            Padding = new Padding(Theme.PageMarginX, Theme.PageMarginTop, Theme.PageMarginX, Theme.PageMarginTop);

            var addButton = new Button
            {
                Text = "＋  Add Customer",
                Width = 168,
                Height = 38,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Accent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            addButton.FlatAppearance.BorderSize = 0;
            addButton.FlatAppearance.MouseOverBackColor = Theme.AccentHover;
            addButton.FlatAppearance.MouseDownBackColor = Theme.AccentPressed;
            Theme.ApplyRoundedRegion(addButton, 8);
            addButton.Click += AddButton_Click;

            var tableCard = Theme.CreateTableCard();

            pendingGrid = Theme.CreateStyledGrid();
            pendingGrid.RowTemplate.Height = 52;

            pendingGrid.Columns.Add("TicketId", "Ticket #");
            pendingGrid.Columns.Add("Customer", "Customer");
            pendingGrid.Columns.Add("Device", "Device");
            pendingGrid.Columns.Add("Contact", "Contact");
            pendingGrid.Columns.Add("Cost", "Est. Cost");

            var statusColumn = new DataGridViewComboBoxColumn
            {
                Name = "Status",
                HeaderText = "Status",
                DataSource = TicketStatus.All,
                FlatStyle = FlatStyle.Flat
            };
            pendingGrid.Columns.Add(statusColumn);

            pendingGrid.Columns["TicketId"].FillWeight = 70;
            pendingGrid.Columns["Customer"].FillWeight = 150;
            pendingGrid.Columns["Device"].FillWeight = 150;
            pendingGrid.Columns["Contact"].FillWeight = 170;
            pendingGrid.Columns["Cost"].FillWeight = 90;
            pendingGrid.Columns["Status"].FillWeight = 120;

            foreach (DataGridViewColumn column in pendingGrid.Columns)
            {
                if (column.Name == "Status") continue;
                column.ReadOnly = true;
            }

            pendingGrid.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (pendingGrid.IsCurrentCellDirty)
                    pendingGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            pendingGrid.CellValueChanged += PendingGrid_CellValueChanged;

            ShowEmptyState();

            tableCard.Controls.Add(pendingGrid);

            toastLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 0,
                BackColor = Theme.StatusRepairedBg,
                ForeColor = Theme.StatusRepairedText,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };
            toastTimer = new System.Windows.Forms.Timer { Interval = 3000 };
            toastTimer.Tick += (s, e) => { toastTimer.Stop(); HideToast(); };

            Controls.Add(tableCard);
            Controls.Add(toastLabel);
            Controls.Add(Theme.CreateSpacer());
            Controls.Add(Theme.CreatePageHeader(
                "Pending Customers", "Walk-in devices currently being diagnosed or repaired", addButton));

            ResumeLayout(false);
        }

        private void ShowEmptyState()
        {
            // Status cell is left null (not "") since it's a combo-box column —
            // an empty string isn't one of its valid items and would throw.
            pendingGrid.Rows.Add("No pending customers.", "", "", "", "");
            pendingGrid.Rows[0].ReadOnly = true;
            pendingGrid.Rows[0].DefaultCellStyle = new DataGridViewCellStyle
            {
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 9.75F, FontStyle.Italic)
            };
        }

        /// <summary>Reloads the grid from the real database. Call on every page navigation.</summary>
        public async Task LoadData()
        {
            pendingGrid.CellValueChanged -= PendingGrid_CellValueChanged;
            pendingGrid.Rows.Clear();

            var pending = await _controller.GetPendingTicketsAsync();

            if (pending.Count == 0)
            {
                ShowEmptyState();
            }
            else
            {
                foreach (var ticket in pending)
                {
                    int rowIndex = pendingGrid.Rows.Add(
                        ticket.TicketId,
                        ticket.FullName,
                        ticket.DeviceName,
                        ticket.PhoneNumber,
                        ticket.EstimatedCost.ToString("C"),
                        ticket.Status);

                    pendingGrid.Rows[rowIndex].Tag = ticket.TicketId;
                }
            }

            pendingGrid.CellValueChanged += PendingGrid_CellValueChanged;
        }

        private async void PendingGrid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (pendingGrid.Columns[e.ColumnIndex].Name != "Status") return;

            var row = pendingGrid.Rows[e.RowIndex];
            if (row.Tag is not int ticketId) return;

            string newStatus = row.Cells["Status"].Value?.ToString() ?? TicketStatus.Diagnose;
            string customerName = row.Cells["Customer"].Value?.ToString() ?? "the customer";

            var (success, message) = await _controller.UpdateTicketStatusAsync(ticketId, newStatus);
            if (!success)
            {
                MessageBox.Show(FindForm(), $"Could not update the status:\n{message}",
                    "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                await LoadData();
                return;
            }

            ShowToast($"✓ {customerName} notified by email & SMS — status is now \"{newStatus}\".");

            // "Repaired" ends the transaction — the ticket leaves this table for Customer History.
            await LoadData();
        }

        private async void AddButton_Click(object? sender, EventArgs e)
        {
            using var dialog = new AddCustomerDialog();
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

            var (success, message, _) = await _controller.AddCustomerAsync(
                dialog.FirstNameValue, dialog.MiddleNameValue, dialog.LastNameValue,
                dialog.PhoneValue, dialog.EmailValue, dialog.DeviceNameValue,
                dialog.ConcernValue, dialog.EstimatedCostValue);

            if (!success)
            {
                MessageBox.Show(FindForm(), $"Could not add the customer:\n{message}",
                    "Add Customer Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            await LoadData();
            ShowToast($"✓ {dialog.FirstNameValue} {dialog.LastNameValue} added to Pending Customers.");
        }

        private void ShowToast(string message)
        {
            toastLabel.Text = message;
            toastLabel.Height = 36;
            toastLabel.Visible = true;
            toastTimer.Stop();
            toastTimer.Start();
        }

        private void HideToast()
        {
            toastLabel.Visible = false;
            toastLabel.Height = 0;
        }
    }
}
