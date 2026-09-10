using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS.winforms.Controllers;

namespace CRMS.winforms.Shared
{
    /// <summary>
    /// Read-only list of completed (Repaired) tickets, with a per-row PDF export.
    /// Used by both the Staff and Admin sidebars so everyone sees the same
    /// finished-transaction history.
    /// </summary>
    public partial class CustomerHistoryControl : UserControl
    {
        private DataGridView historyGrid = null!;

        public CustomerHistoryControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            BackColor = Theme.PageBg;
            Padding = new Padding(Theme.PageMarginX, Theme.PageMarginTop, Theme.PageMarginX, Theme.PageMarginTop);

            var tableCard = Theme.CreateTableCard();

            historyGrid = Theme.CreateStyledGrid();
            historyGrid.ReadOnly = false; // needs to stay false so the button column is clickable
            historyGrid.RowTemplate.Height = 52;

            historyGrid.Columns.Add("TicketId", "Ticket #");
            historyGrid.Columns.Add("Customer", "Customer");
            historyGrid.Columns.Add("Device", "Device");
            historyGrid.Columns.Add("Completed", "Completed");
            historyGrid.Columns.Add("Cost", "Cost");

            var downloadColumn = new DataGridViewButtonColumn
            {
                Name = "Download",
                HeaderText = "",
                Text = "⬇  Download PDF",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat
            };
            historyGrid.Columns.Add(downloadColumn);

            historyGrid.Columns["TicketId"].FillWeight = 70;
            historyGrid.Columns["Customer"].FillWeight = 150;
            historyGrid.Columns["Device"].FillWeight = 150;
            historyGrid.Columns["Completed"].FillWeight = 140;
            historyGrid.Columns["Cost"].FillWeight = 90;
            historyGrid.Columns["Download"].FillWeight = 140;

            foreach (DataGridViewColumn column in historyGrid.Columns)
            {
                if (column.CellTemplate is DataGridViewButtonCell) continue;
                column.DefaultCellStyle.SelectionBackColor = Color.White;
                column.DefaultCellStyle.SelectionForeColor = Theme.TextPrimary;
            }

            historyGrid.CellClick += HistoryGrid_CellClick;

            ShowEmptyState();

            tableCard.Controls.Add(historyGrid);

            Controls.Add(tableCard);
            Controls.Add(Theme.CreateSpacer());
            Controls.Add(Theme.CreatePageHeader("Customer History", "Completed repairs, ready for records or pickup confirmation"));

            ResumeLayout(false);
        }

        private void ShowEmptyState()
        {
            historyGrid.Rows.Add("No completed tickets yet.", "", "", "", "", "");
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
            historyGrid.Rows.Clear();

            if (history.Count == 0)
            {
                ShowEmptyState();
                return;
            }

            foreach (var ticket in history)
            {
                int rowIndex = historyGrid.Rows.Add(
                    ticket.TicketId,
                    ticket.FullName,
                    ticket.DeviceName,
                    ticket.CompletedAt?.ToString("MMM d, yyyy") ?? "-",
                    ticket.EstimatedCost.ToString("C"));

                historyGrid.Rows[rowIndex].Tag = ticket;
            }
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
