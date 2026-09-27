using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS.winforms.Controllers;
using CRMS.winforms.Shared;
using RoundedPanel = CRMS.winforms.RoundedPanel;
using RoundedRect = CRMS.winforms.RoundedRect;

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
        // Staff-only accent, matching StaffMainForm's sidebar/login palette.
        // Local to this file so it doesn't affect Admin/Manager/Superadmin.
        private static readonly Color StaffAccent = Color.FromArgb(14, 165, 233);
        private static readonly Color StaffAccentHover = Color.FromArgb(10, 116, 163);
        private static readonly Color StaffAccentPressed = Color.FromArgb(8, 99, 140);

        private static readonly Dictionary<string, (Color Bg, Color Fg)> StatusPillColors = new()
        {
            [TicketStatus.Diagnose] = (Theme.StatusDiagnoseBg, Theme.StatusDiagnoseText),
            [TicketStatus.AwaitingApproval] = (Theme.StatusAwaitingApprovalBg, Theme.StatusAwaitingApprovalText),
            [TicketStatus.Repairing] = (Theme.StatusRepairingBg, Theme.StatusRepairingText),
            [TicketStatus.Repaired] = (Theme.StatusRepairedBg, Theme.StatusRepairedText),
            [TicketStatus.Cancelled] = (Theme.StatusCancelledBg, Theme.StatusCancelledText),
            [TicketStatus.Unrepairable] = (Theme.StatusUnrepairableBg, Theme.StatusUnrepairableText)
        };
        private static readonly Font StatusPillFont = new Font("Segoe UI", 8.75F, FontStyle.Bold);
        private static readonly Color StaffGridLine = Color.FromArgb(183, 228, 248);

        private static readonly Dictionary<string, (Color Bg, Color Fg)> PaymentPillColors = new()
        {
            ["Unpaid"] = (Theme.StatusUnrepairableBg, Theme.StatusUnrepairableText),
            ["Partial"] = (Theme.StatusDiagnoseBg, Theme.StatusDiagnoseText),
            ["Paid"] = (Theme.StatusRepairedBg, Theme.StatusRepairedText)
        };
        private static readonly string[] PaymentOptions = { "Unpaid", "Partial", "Paid" };

        private readonly ITicketWorkController _controller;

        private DataGridView pendingGrid = null!;
        private Label toastLabel = null!;
        private System.Windows.Forms.Timer toastTimer = null!;

        private Label lblTicketsToday = null!;
        private Label lblAwaitingPickup = null!;
        private Label lblNeedsApproval = null!;

        /// <summary>Used by both Staff and Manager's shells — accepts anything that
        /// does the same ticket work (StaffController or ManagerController).</summary>
        public PendingCustomersControl(ITicketWorkController controller)
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
                BackColor = StaffAccent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            addButton.FlatAppearance.BorderSize = 0;
            addButton.FlatAppearance.MouseOverBackColor = StaffAccentHover;
            addButton.FlatAppearance.MouseDownBackColor = StaffAccentPressed;
            Theme.ApplyRoundedRegion(addButton, 8);
            addButton.Click += AddButton_Click;

            var tableCard = new RoundedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderColor = StaffGridLine,
                CornerRadius = 12,
                Padding = new Padding(10)
            };

            pendingGrid = Theme.CreateStyledGrid();
            pendingGrid.RowTemplate.Height = 52;
            pendingGrid.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            pendingGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            pendingGrid.GridColor = StaffGridLine;

            pendingGrid.Columns.Add("TicketId", "Ticket #");
            pendingGrid.Columns.Add("Customer", "Customer");
            pendingGrid.Columns.Add("Device", "Device");
            pendingGrid.Columns.Add("Contact", "Contact");
            pendingGrid.Columns.Add("Staff", "Handled By");
            pendingGrid.Columns.Add("Branch", "Branch");
            pendingGrid.Columns.Add("Cost", "Est. Cost");

            var paymentColumn = new DataGridViewComboBoxColumn
            {
                Name = "Payment",
                HeaderText = "Payment",
                DataSource = new List<string>(PaymentOptions),
                FlatStyle = FlatStyle.Flat
            };
            pendingGrid.Columns.Add(paymentColumn);

            var statusColumn = new DataGridViewComboBoxColumn
            {
                Name = "Status",
                HeaderText = "Status",
                DataSource = TicketStatus.All,
                FlatStyle = FlatStyle.Flat
            };
            pendingGrid.Columns.Add(statusColumn);

            var editColumn = new DataGridViewButtonColumn
            {
                Name = "Edit",
                HeaderText = "",
                Text = "",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat
            };
            pendingGrid.Columns.Add(editColumn);

            var approvalColumn = new DataGridViewButtonColumn
            {
                Name = "Approval",
                HeaderText = "",
                Text = "📋 Diagnosis & Approval",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat
            };
            pendingGrid.Columns.Add(approvalColumn);

            pendingGrid.Columns["TicketId"].FillWeight = 60;
            pendingGrid.Columns["Customer"].FillWeight = 130;
            pendingGrid.Columns["Device"].FillWeight = 130;
            pendingGrid.Columns["Contact"].FillWeight = 150;
            pendingGrid.Columns["Staff"].FillWeight = 110;
            pendingGrid.Columns["Branch"].FillWeight = 100;
            pendingGrid.Columns["Cost"].FillWeight = 80;
            pendingGrid.Columns["Payment"].FillWeight = 100;
            pendingGrid.Columns["Status"].FillWeight = 110;
            pendingGrid.Columns["Edit"].FillWeight = 55;
            pendingGrid.Columns["Approval"].FillWeight = 160;

            foreach (DataGridViewColumn column in pendingGrid.Columns)
            {
                if (column.Name is "Status" or "Payment") continue;
                if (column.CellTemplate is DataGridViewButtonCell) continue;
                column.ReadOnly = true;
            }

            pendingGrid.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (pendingGrid.IsCurrentCellDirty)
                    pendingGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            pendingGrid.CellValueChanged += PendingGrid_CellValueChanged;
            pendingGrid.CellPainting += PendingGrid_CellPainting;
            pendingGrid.EditingControlShowing += PendingGrid_EditingControlShowing;
            pendingGrid.CellClick += PendingGrid_CellClick;

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
            Controls.Add(BuildTodayStrip());
            Controls.Add(Theme.CreateSpacer());
            Controls.Add(Theme.CreatePageHeader(
                "Pending Customers", "Walk-in devices currently being diagnosed or repaired", addButton, showTitle: false, showSubtitle: false));

            ResumeLayout(false);
        }

        /// <summary>Slim, quiet strip of tenant-wide counts — not a KPI dashboard,
        /// just enough for Staff to gauge the day at a glance. Text is filled in
        /// by LoadData(); shows "—" until then.</summary>
        private Panel BuildTodayStrip()
        {
            var strip = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = Color.FromArgb(240, 249, 255)
            };
            strip.Paint += (s, e) =>
            {
                using var pen = new Pen(StaffGridLine, 1);
                e.Graphics.DrawLine(pen, 0, strip.Height - 1, strip.Width, strip.Height - 1);
            };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(4, 10, 0, 0)
            };

            lblTicketsToday = AddStripLabel(flow, "\U0001F3AB", "— tickets today");
            AddStripDot(flow);
            lblAwaitingPickup = AddStripLabel(flow, "\U0001F4E6", "— awaiting pickup");
            AddStripDot(flow);
            lblNeedsApproval = AddStripLabel(flow, "⏰", "— needs approval");

            strip.Controls.Add(flow);
            return strip;
        }

        private static Label AddStripLabel(FlowLayoutPanel flow, string icon, string text)
        {
            var label = new Label
            {
                Text = $"{icon}  {text}",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Theme.TextPrimary,
                Margin = new Padding(0, 2, 18, 0)
            };
            flow.Controls.Add(label);
            return label;
        }

        private static void AddStripDot(FlowLayoutPanel flow)
        {
            flow.Controls.Add(new Label
            {
                Text = "·",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Theme.TextSecondary,
                Margin = new Padding(0, 2, 18, 0)
            });
        }

        private void ShowEmptyState()
        {
            // Status cell is left null (not "") since it's a combo-box column —
            // an empty string isn't one of its valid items and would throw.
            pendingGrid.Rows.Add("No pending customers.", "", "", "", "", "", "");
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

            var pendingTask = _controller.GetPendingTicketsAsync();
            var summaryTask = _controller.GetTodaySummaryAsync();
            await Task.WhenAll(pendingTask, summaryTask);

            var pending = pendingTask.Result;
            var summary = summaryTask.Result;
            lblTicketsToday.Text = $"\U0001F3AB  {summary.TicketsToday} ticket{(summary.TicketsToday == 1 ? "" : "s")} today";
            lblAwaitingPickup.Text = $"\U0001F4E6  {summary.AwaitingPickup} awaiting pickup";
            lblNeedsApproval.Text = $"⏰  {summary.NeedsApproval} needs approval";

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
                        string.IsNullOrWhiteSpace(ticket.StaffName) ? "—" : ticket.StaffName,
                        string.IsNullOrWhiteSpace(ticket.BranchName) ? "—" : ticket.BranchName,
                        ticket.EstimatedCost.ToString("₱#,##0.00"),
                        ticket.PaymentStatus,
                        ticket.Status);

                    pendingGrid.Rows[rowIndex].Tag = ticket;

                    // Once fully paid, that's a closed financial fact — lock the
                    // cell so it can't be walked back to Partial/Unpaid (the API
                    // enforces this too; this just avoids the pointless round trip).
                    if (ticket.PaymentStatus == "Paid")
                        pendingGrid.Rows[rowIndex].Cells["Payment"].ReadOnly = true;
                }
            }

            pendingGrid.CellValueChanged += PendingGrid_CellValueChanged;
        }

        private async void PendingGrid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string columnName = pendingGrid.Columns[e.ColumnIndex].Name;
            if (columnName != "Status" && columnName != "Payment") return;

            var row = pendingGrid.Rows[e.RowIndex];
            if (row.Tag is not TicketRecord ticket) return;

            if (columnName == "Status")
            {
                string newStatus = row.Cells["Status"].Value?.ToString() ?? TicketStatus.Diagnose;
                string customerName = row.Cells["Customer"].Value?.ToString() ?? "the customer";

                var (success, message) = await _controller.UpdateTicketStatusAsync(ticket.TicketId, newStatus);
                if (!success)
                {
                    MessageBox.Show(FindForm(), $"Could not update the status:\n{message}",
                        "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    await LoadData();
                    return;
                }

                ShowToast($"✓ {customerName} notified by email & SMS — status is now \"{newStatus}\".");

                // Repaired/Cancelled/Unrepairable all end the transaction — the ticket
                // leaves this table for Customer History (the API's own pending/history
                // filter decides this; LoadData() just re-fetches and the row disappears).
                await LoadData();
                return;
            }

            // Payment: a deposit or partial payment can come in while the device
            // is still being worked on — Staff shouldn't have to wait until the
            // ticket lands in Customer History to record it.
            string newPayment = row.Cells["Payment"].Value?.ToString() ?? "Unpaid";

            decimal? amountToSend = null;
            if (newPayment == "Partial")
            {
                using var amountDialog = new AmountInputDialog(ticket.EstimatedCost, ticket.AmountPaid);
                if (amountDialog.ShowDialog(FindForm()) != DialogResult.OK)
                {
                    await LoadData(); // revert the combo back to its saved value
                    return;
                }
                amountToSend = amountDialog.AmountValue;
            }

            var (paymentSuccess, paymentMessage) = await _controller.UpdatePaymentStatusAsync(ticket.TicketId, newPayment, amountToSend);
            if (!paymentSuccess)
            {
                MessageBox.Show(FindForm(), $"Could not update payment status:\n{paymentMessage}",
                    "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            await LoadData();
        }

        /// <summary>Draws the Status/Payment columns as colored pills (matching each
        /// value's meaning) instead of plain combo-box text. Purely cosmetic — the
        /// real dropdown editor still opens exactly as before when clicked.</summary>
        private void PendingGrid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string columnName = pendingGrid.Columns[e.ColumnIndex].Name;

            if (columnName == "Edit")
            {
                e.PaintBackground(e.CellBounds, true);
                IconResources.DrawCentered(e.Graphics!, "edit", e.CellBounds);
                e.Handled = true;
                return;
            }

            var colorMap = columnName switch
            {
                "Status" => StatusPillColors,
                "Payment" => PaymentPillColors,
                _ => null
            };
            if (colorMap is null) return;

            string value = e.Value?.ToString() ?? string.Empty;
            if (!colorMap.TryGetValue(value, out var colors)) return;

            e.PaintBackground(e.CellBounds, true);
            e.Graphics!.SmoothingMode = SmoothingMode.AntiAlias;

            var pillRect = Rectangle.Inflate(e.CellBounds, -12, -10);
            using (var path = RoundedRect.Path(pillRect, pillRect.Height / 2))
            using (var brush = new SolidBrush(colors.Bg))
            {
                e.Graphics.FillPath(brush, path);
            }
            TextRenderer.DrawText(e.Graphics, value, StatusPillFont, pillRect, colors.Fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            e.Handled = true;
        }

        /// <summary>Restyles the real dropdown list that opens when a Status/Payment
        /// cell is clicked, so it matches the same colored-pill look instead of the
        /// plain system list (default font/blue-highlight combo box).</summary>
        private void PendingGrid_EditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
        {
            string? columnName = pendingGrid.CurrentCell?.OwningColumn?.Name;
            if (columnName != "Status" && columnName != "Payment") return;
            if (e.Control is not ComboBox combo) return;

            combo.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            combo.FlatStyle = FlatStyle.Flat;
            combo.DrawMode = DrawMode.OwnerDrawFixed;
            combo.ItemHeight = 30;
            combo.DrawItem -= StatusCombo_DrawItem;
            combo.DrawItem += StatusCombo_DrawItem;
            combo.Tag = columnName;
        }

        private void StatusCombo_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (sender is not ComboBox combo || e.Index < 0) return;

            var colorMap = (combo.Tag as string) == "Payment" ? PaymentPillColors : StatusPillColors;
            string status = combo.Items[e.Index]?.ToString() ?? string.Empty;
            var (bg, fg) = colorMap.TryGetValue(status, out var colors)
                ? colors
                : (Color.White, Theme.TextPrimary);

            bool highlighted = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            using (var bgBrush = new SolidBrush(bg))
            {
                e.Graphics.FillRectangle(bgBrush, e.Bounds);
            }
            if (highlighted)
            {
                using var accentPen = new Pen(fg, 3);
                e.Graphics.DrawLine(accentPen, e.Bounds.Left + 1, e.Bounds.Top + 3, e.Bounds.Left + 1, e.Bounds.Bottom - 3);
            }

            var textRect = new Rectangle(e.Bounds.X + 16, e.Bounds.Y, e.Bounds.Width - 16, e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, status, combo.Font, textRect, fg,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
        }

        private async void PendingGrid_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var row = pendingGrid.Rows[e.RowIndex];
            if (row.Tag is not TicketRecord ticket) return;

            string columnName = pendingGrid.Columns[e.ColumnIndex].Name;

            if (columnName == "Edit")
            {
                using var dialog = new EditTicketDialog(
                    ticket.FullName, ticket.PhoneNumber, ticket.Email,
                    ticket.DeviceName, ticket.Concern, ticket.EstimatedCost, ticket.SelectedParts,
                    _controller.GetApprovedPromotionsAsync, ticket.PromotionId, ticket.PromotionTitle, ticket.OriginalPrice);
                if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

                var (success, message) = await _controller.UpdateTicketDetailsAsync(
                    ticket.TicketId, dialog.FullNameValue, dialog.PhoneValue, dialog.EmailValue,
                    dialog.DeviceNameValue, dialog.ConcernValue, dialog.EstimatedCostValue, dialog.SelectedPartsValue,
                    dialog.PromotionIdValue, dialog.ClearedPromotion, dialog.OriginalPriceValue);

                if (!success)
                {
                    MessageBox.Show(FindForm(), $"Could not update the ticket:\n{message}",
                        "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                await LoadData();
                ShowToast($"✓ Ticket #{ticket.TicketId} updated.");
            }
            else if (columnName == "Approval")
            {
                using var dialog = new RecordApprovalDialog(
                    ticket.DiagnosisFindings, ticket.EstimatedCost, ticket.ApprovalStatus);
                if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

                var (success, message) = await _controller.RecordApprovalAsync(
                    ticket.TicketId, dialog.FindingsValue, dialog.EstimatedCostValue, dialog.DecisionValue,
                    dialog.ApprovedAmountValue, dialog.MethodValue);

                if (!success)
                {
                    MessageBox.Show(FindForm(), $"Could not save the diagnosis/approval:\n{message}",
                        "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                await LoadData();
                ShowToast(dialog.DecisionValue switch
                {
                    "Approved" => $"✓ Ticket #{ticket.TicketId} approved — moved to Repairing.",
                    "Rejected" => $"✓ Ticket #{ticket.TicketId} rejected — moved to Cancelled.",
                    _ => $"✓ Findings saved for Ticket #{ticket.TicketId}."
                });
            }
        }

        private async void AddButton_Click(object? sender, EventArgs e)
        {
            using var dialog = new AddCustomerDialog(_controller.SearchCustomersAsync, _controller.GetApprovedPromotionsAsync, _controller.GetCurrentTermsAsync);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

            var (success, message, _) = await _controller.AddCustomerAsync(
                dialog.FirstNameValue, dialog.MiddleNameValue, dialog.LastNameValue,
                dialog.PhoneValue, dialog.EmailValue, dialog.DeviceNameValue,
                dialog.ConcernValue, dialog.EstimatedCostValue, dialog.SelectedPartsValue,
                dialog.ExistingCustomerIdValue, dialog.PromotionIdValue, dialog.OriginalPriceValue);

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
