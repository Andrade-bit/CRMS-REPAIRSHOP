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

namespace CRMS.Winforms.Admin
{
    /// <summary>
    /// Admin's Promotions page: every offer submitted across the tenant by any
    /// Manager, with Approve/Reject actions on anything still Pending Approval.
    /// Nothing here actually reaches a customer — that would be a later step.
    /// </summary>
    public partial class PromotionsReviewControl : UserControl
    {
        private static readonly Color GridLine = Color.FromArgb(241, 245, 249);
        private static readonly Color TableBorder = Color.FromArgb(209, 213, 219);
        private static readonly Color RowHoverColor = Color.FromArgb(240, 249, 255);
        private static readonly Color RowAlternateColor = Color.FromArgb(249, 250, 251);
        private static readonly Color RowSelectedColor = Color.FromArgb(234, 244, 251);
        private static readonly Color ApproveColor = Color.FromArgb(24, 130, 80);
        private static readonly Color ApproveHoverColor = Color.FromArgb(19, 107, 66);
        private static readonly Color RejectColor = Color.FromArgb(163, 45, 45);
        private static readonly Color RejectHoverColor = Color.FromArgb(138, 36, 36);
        private static readonly Color ArchiveColor = Color.FromArgb(120, 128, 140);
        private static readonly Color ArchiveHoverColor = Color.FromArgb(96, 103, 114);

        private static readonly Dictionary<string, (Color Bg, Color Fg)> StatusPillColors = new()
        {
            [PromotionStatus.Draft] = (Color.FromArgb(236, 238, 241), Color.FromArgb(91, 107, 122)),
            [PromotionStatus.PendingApproval] = (Theme.StatusDiagnoseBg, Theme.StatusDiagnoseText),
            [PromotionStatus.Approved] = (Theme.StatusRepairedBg, Theme.StatusRepairedText),
            [PromotionStatus.Rejected] = (Color.FromArgb(251, 222, 222), Color.FromArgb(163, 45, 45)),
        };
        private static readonly Font StatusPillFont = new Font("Segoe UI", 8.75F, FontStyle.Bold);
        private static readonly Font ActionIconFont = new Font("Segoe UI", 11F, FontStyle.Bold);

        private readonly AdminController _controller;

        private DataGridView promotionsGrid = null!;
        private Label toastLabel = null!;
        private System.Windows.Forms.Timer toastTimer = null!;
        private int _hoverRowIndex = -1;
        private int _hoverColumnIndex = -1;

        public PromotionsReviewControl(AdminController controller)
        {
            _controller = controller;
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
                BorderColor = TableBorder,
                CornerRadius = 12,
                Padding = new Padding(10)
            };

            promotionsGrid = Theme.CreateStyledGrid();
            promotionsGrid.ReadOnly = false; // needs to stay false so the button columns are clickable
            promotionsGrid.RowTemplate.Height = 56;
            promotionsGrid.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            promotionsGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            promotionsGrid.GridColor = GridLine;
            promotionsGrid.ColumnHeadersDefaultCellStyle.Padding = new Padding(20, 0, 20, 0);
            promotionsGrid.DefaultCellStyle.Padding = new Padding(20, 0, 20, 0);
            promotionsGrid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = RowAlternateColor };

            promotionsGrid.Columns.Add("Title", "Title");
            promotionsGrid.Columns.Add("Message", "Message");
            promotionsGrid.Columns.Add("Reason", "Reason");
            promotionsGrid.Columns.Add("SubmittedBy", "Submitted By");
            promotionsGrid.Columns.Add("Status", "Status");

            var approveColumn = new DataGridViewButtonColumn
            {
                Name = "Approve",
                HeaderText = "",
                Text = "✓",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat
            };
            promotionsGrid.Columns.Add(approveColumn);

            var rejectColumn = new DataGridViewButtonColumn
            {
                Name = "Reject",
                HeaderText = "",
                Text = "✕",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat
            };
            promotionsGrid.Columns.Add(rejectColumn);

            var archiveColumn = new DataGridViewButtonColumn
            {
                Name = "Archive",
                HeaderText = "",
                Text = "\U0001F5C4",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat
            };
            promotionsGrid.Columns.Add(archiveColumn);

            promotionsGrid.Columns["Title"].FillWeight = 110;
            promotionsGrid.Columns["Message"].FillWeight = 190;
            promotionsGrid.Columns["Reason"].FillWeight = 190;
            promotionsGrid.Columns["SubmittedBy"].FillWeight = 110;
            promotionsGrid.Columns["Status"].FillWeight = 110;
            promotionsGrid.Columns["Approve"].FillWeight = 55;
            promotionsGrid.Columns["Reject"].FillWeight = 55;
            promotionsGrid.Columns["Archive"].FillWeight = 55;

            foreach (DataGridViewColumn column in promotionsGrid.Columns)
            {
                if (column.CellTemplate is DataGridViewButtonCell) continue;
                column.ReadOnly = true;
                column.DefaultCellStyle.SelectionBackColor = RowSelectedColor;
                column.DefaultCellStyle.SelectionForeColor = Theme.TextPrimary;
            }
            promotionsGrid.Columns["Message"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            promotionsGrid.Columns["Reason"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            promotionsGrid.CellPainting += PromotionsGrid_CellPainting;
            promotionsGrid.CellClick += PromotionsGrid_CellClick;

            // Hover highlight — row-wide tint, plus the Approve/Reject circles
            // darken individually when the mouse is directly over them.
            promotionsGrid.CellMouseMove += (s, e) =>
            {
                if (e.RowIndex == _hoverRowIndex && e.ColumnIndex == _hoverColumnIndex) return;
                int oldRow = _hoverRowIndex, oldCol = _hoverColumnIndex;
                _hoverRowIndex = e.RowIndex;
                _hoverColumnIndex = e.ColumnIndex;
                SetRowHover(oldRow, false);
                SetRowHover(_hoverRowIndex, true);
                InvalidateActionCell(oldRow, oldCol);
                InvalidateActionCell(_hoverRowIndex, _hoverColumnIndex);
            };
            promotionsGrid.MouseLeave += (s, e) =>
            {
                int oldRow = _hoverRowIndex, oldCol = _hoverColumnIndex;
                SetRowHover(_hoverRowIndex, false);
                _hoverRowIndex = -1;
                _hoverColumnIndex = -1;
                InvalidateActionCell(oldRow, oldCol);
            };

            promotionsGrid.RowPostPaint += (s, e) =>
            {
                if (!promotionsGrid.Rows[e.RowIndex].Selected) return;
                using var brush = new SolidBrush(Theme.Accent);
                e.Graphics.FillRectangle(brush, e.RowBounds.Left, e.RowBounds.Top, 3, e.RowBounds.Height);
            };

            ShowEmptyState();
            tableCard.Controls.Add(promotionsGrid);

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
            Controls.Add(Theme.CreatePageHeader("Promotions", "Review offers submitted by your Managers before they go out", showTitle: false, showSubtitle: false));

            ResumeLayout(false);
        }

        private void ShowEmptyState()
        {
            promotionsGrid.Rows.Add("No promotions submitted yet.", "", "", "", "", "", "", "");
            promotionsGrid.Rows[0].ReadOnly = true;
            promotionsGrid.Rows[0].DefaultCellStyle = new DataGridViewCellStyle
            {
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 9.75F, FontStyle.Italic)
            };
        }

        /// <summary>Reloads every promotion across the tenant. Call on every page navigation.</summary>
        public async Task LoadData()
        {
            promotionsGrid.Rows.Clear();

            var promotions = await _controller.GetAllPromotionsAsync();

            if (promotions.Count == 0)
            {
                ShowEmptyState();
                return;
            }

            foreach (var promo in promotions)
            {
                int rowIndex = promotionsGrid.Rows.Add(
                    promo.Title,
                    promo.Message,
                    string.IsNullOrWhiteSpace(promo.Reason) ? "—" : promo.Reason,
                    promo.StaffName,
                    promo.Status == PromotionStatus.PendingApproval ? "Pending Approval" : promo.Status,
                    "", "", "\U0001F5C4");

                promotionsGrid.Rows[rowIndex].Tag = promo;

                // Approve/Reject only make sense while it's still awaiting a decision.
                // Archive is always available, whatever the status.
                bool decided = promo.Status != PromotionStatus.PendingApproval && promo.Status != PromotionStatus.Draft;
                promotionsGrid.Rows[rowIndex].Cells["Approve"].ReadOnly = decided;
                promotionsGrid.Rows[rowIndex].Cells["Reject"].ReadOnly = decided;
                if (decided)
                {
                    promotionsGrid.Rows[rowIndex].Cells["Approve"].Value = "";
                    promotionsGrid.Rows[rowIndex].Cells["Reject"].Value = "";
                }
            }
        }

        private async void PromotionsGrid_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var row = promotionsGrid.Rows[e.RowIndex];
            if (row.Tag is not PromotionRecord promo) return;
            if (row.Cells[e.ColumnIndex].ReadOnly) return;

            string columnName = promotionsGrid.Columns[e.ColumnIndex].Name;

            if (columnName == "Archive")
            {
                var confirm = MessageBox.Show(FindForm(), $"Archive \"{promo.Title}\"? It will be hidden from this list, but the record is kept.",
                    "Archive Promotion", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes) return;

                var (archived, archiveMessage) = await _controller.ArchivePromotionAsync(promo.PromotionId);
                if (!archived)
                {
                    MessageBox.Show(FindForm(), $"Could not archive this promotion:\n{archiveMessage}",
                        "Archive Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                await LoadData();
                ShowToast($"✓ \"{promo.Title}\" archived.");
                return;
            }

            if (columnName != "Approve" && columnName != "Reject") return;

            bool approve = columnName == "Approve";
            var (success, message) = approve
                ? await _controller.ApprovePromotionAsync(promo.PromotionId)
                : await _controller.RejectPromotionAsync(promo.PromotionId);

            if (!success)
            {
                MessageBox.Show(FindForm(), $"Could not {(approve ? "approve" : "reject")} this promotion:\n{message}",
                    "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            await LoadData();
            ShowToast(approve
                ? $"✓ \"{promo.Title}\" approved."
                : $"✓ \"{promo.Title}\" rejected.");
        }

        /// <summary>Draws the Status column as a colored pill, and the
        /// Approve/Reject buttons as small circular icon buttons instead of
        /// the default flat rectangle — an emoji-style ✓/✕ is all they need.</summary>
        private void PromotionsGrid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string columnName = promotionsGrid.Columns[e.ColumnIndex].Name;

            if (columnName == "Status")
            {
                string status = e.Value?.ToString() ?? string.Empty;
                string key = status.Replace(" ", "");
                if (!StatusPillColors.TryGetValue(key, out var colors)) return;

                e.PaintBackground(e.CellBounds, true);
                e.Graphics!.SmoothingMode = SmoothingMode.AntiAlias;

                var pillRect = Rectangle.Inflate(e.CellBounds, -12, -12);
                using (var path = RoundedRect.Path(pillRect, pillRect.Height / 2))
                using (var brush = new SolidBrush(colors.Bg))
                {
                    e.Graphics.FillPath(brush, path);
                }
                TextRenderer.DrawText(e.Graphics, status, StatusPillFont, pillRect, colors.Fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                e.Handled = true;
            }
            else if (columnName is "Approve" or "Archive")
            {
                e.PaintBackground(e.CellBounds, true);

                string val = e.Value?.ToString() ?? string.Empty;
                if (val.Length == 0) { e.Handled = true; return; }

                IconResources.DrawCentered(e.Graphics!, columnName == "Approve" ? "approve" : "archive", e.CellBounds);
                e.Handled = true;
            }
            else if (columnName == "Reject")
            {
                e.PaintBackground(e.CellBounds, true);

                string symbol = e.Value?.ToString() ?? string.Empty;
                if (symbol.Length == 0) { e.Handled = true; return; }

                e.Graphics!.SmoothingMode = SmoothingMode.AntiAlias;

                bool hovered = e.RowIndex == _hoverRowIndex && e.ColumnIndex == _hoverColumnIndex;
                Color circleColor = hovered ? RejectHoverColor : RejectColor;

                int diameter = 30;
                var circleRect = new Rectangle(
                    e.CellBounds.X + (e.CellBounds.Width - diameter) / 2,
                    e.CellBounds.Y + (e.CellBounds.Height - diameter) / 2,
                    diameter, diameter);

                using (var brush = new SolidBrush(circleColor))
                    e.Graphics.FillEllipse(brush, circleRect);

                TextRenderer.DrawText(e.Graphics, symbol, ActionIconFont, circleRect, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                e.Handled = true;
            }
        }

        private void SetRowHover(int rowIndex, bool hovered)
        {
            if (rowIndex < 0 || rowIndex >= promotionsGrid.Rows.Count) return;
            promotionsGrid.Rows[rowIndex].DefaultCellStyle.BackColor = hovered ? RowHoverColor : Color.Empty;
        }

        private void InvalidateActionCell(int rowIndex, int columnIndex)
        {
            if (rowIndex < 0 || rowIndex >= promotionsGrid.Rows.Count) return;
            if (columnIndex < 0 || columnIndex >= promotionsGrid.Columns.Count) return;
            string columnName = promotionsGrid.Columns[columnIndex].Name;
            if (columnName is "Approve" or "Reject" or "Archive")
                promotionsGrid.InvalidateCell(columnIndex, rowIndex);
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
