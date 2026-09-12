using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS.winforms.Controllers;
using RoundedPanel = CRMS.winforms.RoundedPanel;
using RoundedRect = CRMS.winforms.RoundedRect;

namespace CRMS.winforms.Shared
{
    /// <summary>
    /// Read-only list of completed (Repaired) tickets, with a per-row PDF export
    /// and a Pickup toggle. Used by both the Staff and Admin sidebars so everyone
    /// sees the same finished-transaction history.
    /// </summary>
    public partial class CustomerHistoryControl : UserControl
    {
        private static readonly Color GridLine = Color.FromArgb(183, 228, 248);
        private static readonly Font StatusPillFont = new Font("Segoe UI", 8.75F, FontStyle.Bold);

        private const string NotYet = "Not Yet";
        private const string PickedUp = "Picked Up";
        private static readonly string[] PickupOptions = { NotYet, PickedUp };

        private static readonly Dictionary<string, (Color Bg, Color Fg)> PickupPillColors = new()
        {
            [NotYet] = (Theme.StatusDiagnoseBg, Theme.StatusDiagnoseText),
            [PickedUp] = (Theme.StatusRepairedBg, Theme.StatusRepairedText)
        };

        private DataGridView historyGrid = null!;

        /// <summary>Set by the host page (Staff/Admin) to actually persist a pickup
        /// change. Called with (ticketId, pickedUp); returns (Success, Message).</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<int, bool, Task<(bool Success, string Message)>>? OnPickupChanged { get; set; }

        public CustomerHistoryControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            BackColor = Theme.PageBg;
            Padding = new Padding(Theme.PageMarginX, Theme.PageMarginTop, Theme.PageMarginX, Theme.PageMarginTop);

            var tableCard = new RoundedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderColor = GridLine,
                CornerRadius = 12,
                Padding = new Padding(10)
            };

            historyGrid = Theme.CreateStyledGrid();
            historyGrid.ReadOnly = false; // needs to stay false so the button column is clickable
            historyGrid.RowTemplate.Height = 52;
            historyGrid.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            historyGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            historyGrid.GridColor = GridLine;

            historyGrid.Columns.Add("TicketId", "Ticket #");
            historyGrid.Columns.Add("Customer", "Customer");
            historyGrid.Columns.Add("Device", "Device");
            historyGrid.Columns.Add("HandledBy", "Handled By");
            historyGrid.Columns.Add("Status", "Status");
            historyGrid.Columns.Add("CreatedAt", "Created At");
            historyGrid.Columns.Add("Completed", "Completed At");
            historyGrid.Columns.Add("Cost", "Cost");

            var pickupColumn = new DataGridViewComboBoxColumn
            {
                Name = "Pickup",
                HeaderText = "Picked Up",
                DataSource = PickupOptions,
                FlatStyle = FlatStyle.Flat
            };
            historyGrid.Columns.Add(pickupColumn);

            var downloadColumn = new DataGridViewButtonColumn
            {
                Name = "Download",
                HeaderText = "",
                Text = "⬇  Download PDF",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat
            };
            historyGrid.Columns.Add(downloadColumn);

            historyGrid.Columns["TicketId"].FillWeight = 55;
            historyGrid.Columns["Customer"].FillWeight = 120;
            historyGrid.Columns["Device"].FillWeight = 110;
            historyGrid.Columns["HandledBy"].FillWeight = 100;
            historyGrid.Columns["Status"].FillWeight = 90;
            historyGrid.Columns["CreatedAt"].FillWeight = 100;
            historyGrid.Columns["Completed"].FillWeight = 100;
            historyGrid.Columns["Cost"].FillWeight = 75;
            historyGrid.Columns["Pickup"].FillWeight = 100;
            historyGrid.Columns["Download"].FillWeight = 140;

            foreach (DataGridViewColumn column in historyGrid.Columns)
            {
                if (column.Name == "Pickup") continue;
                if (column.CellTemplate is DataGridViewButtonCell) continue;
                column.ReadOnly = true;
                column.DefaultCellStyle.SelectionBackColor = Color.White;
                column.DefaultCellStyle.SelectionForeColor = Theme.TextPrimary;
            }

            historyGrid.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (historyGrid.IsCurrentCellDirty)
                    historyGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            historyGrid.CellValueChanged += HistoryGrid_CellValueChanged;
            historyGrid.CellClick += HistoryGrid_CellClick;
            historyGrid.CellPainting += HistoryGrid_CellPainting;
            historyGrid.EditingControlShowing += HistoryGrid_EditingControlShowing;

            ShowEmptyState();

            tableCard.Controls.Add(historyGrid);

            Controls.Add(tableCard);
            Controls.Add(Theme.CreateSpacer());
            Controls.Add(Theme.CreatePageHeader("Customer History", "Completed repairs, ready for records or pickup confirmation"));

            ResumeLayout(false);
        }

        private void ShowEmptyState()
        {
            // Pickup is a combo-box column — an empty string isn't a valid item
            // and would throw, so it's left unset here (same reasoning as the
            // Status combo column on the Pending Customers page).
            historyGrid.Rows.Add("No completed tickets yet.", "", "", "", "", "", "", "");
            historyGrid.Rows[0].ReadOnly = true;
            historyGrid.Rows[0].DefaultCellStyle = new DataGridViewCellStyle
            {
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 9.75F, FontStyle.Italic)
            };
        }

        /// <summary>Reloads the grid from the given ticket history (call on every page navigation).</summary>
        public void LoadData(System.Collections.Generic.IReadOnlyList<TicketRecord> history)
        {
            historyGrid.CellValueChanged -= HistoryGrid_CellValueChanged;
            historyGrid.Rows.Clear();

            if (history.Count == 0)
            {
                ShowEmptyState();
            }
            else
            {
                foreach (var ticket in history)
                {
                    int rowIndex = historyGrid.Rows.Add(
                        ticket.TicketId,
                        ticket.FullName,
                        ticket.DeviceName,
                        string.IsNullOrWhiteSpace(ticket.StaffName) ? "-" : ticket.StaffName,
                        "Completed",
                        ticket.CreatedAt.ToString("MMM d, yyyy"),
                        ticket.CompletedAt?.ToString("MMM d, yyyy") ?? "-",
                        ticket.EstimatedCost.ToString("C"),
                        ticket.PickedUpAt.HasValue ? PickedUp : NotYet);

                    historyGrid.Rows[rowIndex].Tag = ticket;
                }
            }

            historyGrid.CellValueChanged += HistoryGrid_CellValueChanged;
        }

        private async void HistoryGrid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (historyGrid.Columns[e.ColumnIndex].Name != "Pickup") return;
            if (OnPickupChanged is null) return;

            var row = historyGrid.Rows[e.RowIndex];
            if (row.Tag is not TicketRecord ticket) return;

            string newValue = row.Cells["Pickup"].Value?.ToString() ?? NotYet;
            bool pickedUp = newValue == PickedUp;

            var (success, message) = await OnPickupChanged(ticket.TicketId, pickedUp);
            if (!success)
            {
                MessageBox.Show(FindForm(), $"Could not update pickup status:\n{message}",
                    "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);

                historyGrid.CellValueChanged -= HistoryGrid_CellValueChanged;
                row.Cells["Pickup"].Value = pickedUp ? NotYet : PickedUp;
                historyGrid.CellValueChanged += HistoryGrid_CellValueChanged;
            }
        }

        /// <summary>Draws the Status column as a colored "Completed" pill, and the
        /// Pickup column as a colored pill matching each option's meaning, both
        /// matching the Status pill style used on the Pending Customers page.</summary>
        private void HistoryGrid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string columnName = historyGrid.Columns[e.ColumnIndex].Name;

            (Color Bg, Color Fg) colors;
            string text = e.Value?.ToString() ?? string.Empty;

            if (columnName == "Status")
            {
                if (string.IsNullOrEmpty(text)) return;
                colors = (Theme.StatusRepairedBg, Theme.StatusRepairedText);
            }
            else if (columnName == "Pickup")
            {
                if (!PickupPillColors.TryGetValue(text, out colors)) return;
            }
            else
            {
                return;
            }

            e.PaintBackground(e.CellBounds, true);
            e.Graphics!.SmoothingMode = SmoothingMode.AntiAlias;

            var pillRect = Rectangle.Inflate(e.CellBounds, -12, -10);
            using (var path = RoundedRect.Path(pillRect, pillRect.Height / 2))
            using (var brush = new SolidBrush(colors.Bg))
            {
                e.Graphics.FillPath(brush, path);
            }
            TextRenderer.DrawText(e.Graphics, text, StatusPillFont, pillRect, colors.Fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            e.Handled = true;
        }

        /// <summary>Restyles the real dropdown list that opens when a Pickup cell is
        /// clicked, matching the colored-pill styling used on Pending Customers.</summary>
        private void HistoryGrid_EditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (historyGrid.CurrentCell?.OwningColumn?.Name != "Pickup") return;
            if (e.Control is not ComboBox combo) return;

            combo.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            combo.FlatStyle = FlatStyle.Flat;
            combo.DrawMode = DrawMode.OwnerDrawFixed;
            combo.ItemHeight = 30;
            combo.DrawItem -= PickupCombo_DrawItem;
            combo.DrawItem += PickupCombo_DrawItem;
        }

        private void PickupCombo_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (sender is not ComboBox combo || e.Index < 0) return;

            string text = combo.Items[e.Index]?.ToString() ?? string.Empty;
            var (bg, fg) = PickupPillColors.TryGetValue(text, out var colors)
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
            TextRenderer.DrawText(e.Graphics, text, combo.Font, textRect, fg,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
        }

        private void HistoryGrid_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (historyGrid.Columns[e.ColumnIndex].Name != "Download") return;

            var row = historyGrid.Rows[e.RowIndex];
            if (row.Tag is not TicketRecord ticket) return;

            using var dialog = new SaveFileDialog
            {
                Title = "Save Customer History PDF",
                Filter = "PDF Document|*.pdf",
                FileName = $"Ticket-{ticket.TicketId}-{ticket.FullName.Replace(' ', '_')}.pdf"
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                PdfExportService.ExportTicketHistory(ticket, dialog.FileName);
                MessageBox.Show(this, "PDF saved successfully.", "Export Complete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not save the PDF:\n{ex.Message}", "Export Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
