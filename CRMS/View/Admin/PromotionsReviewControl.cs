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
        private static readonly Color GridLine = Color.FromArgb(183, 228, 248);

        private static readonly Dictionary<string, (Color Bg, Color Fg)> StatusPillColors = new()
        {
            [PromotionStatus.Draft] = (Color.FromArgb(236, 238, 241), Color.FromArgb(91, 107, 122)),
            [PromotionStatus.PendingApproval] = (Theme.StatusDiagnoseBg, Theme.StatusDiagnoseText),
            [PromotionStatus.Approved] = (Theme.StatusRepairedBg, Theme.StatusRepairedText),
            [PromotionStatus.Rejected] = (Color.FromArgb(251, 222, 222), Color.FromArgb(163, 45, 45)),
        };
        private static readonly Font StatusPillFont = new Font("Segoe UI", 8.75F, FontStyle.Bold);

        private readonly AdminController _controller;

        private DataGridView promotionsGrid = null!;
        private Label toastLabel = null!;
        private System.Windows.Forms.Timer toastTimer = null!;

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
                BorderColor = GridLine,
                CornerRadius = 12,
                Padding = new Padding(10)
            };

            promotionsGrid = Theme.CreateStyledGrid();
            promotionsGrid.ReadOnly = false; // needs to stay false so the button columns are clickable
            promotionsGrid.RowTemplate.Height = 56;
            promotionsGrid.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            promotionsGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            promotionsGrid.GridColor = GridLine;

            promotionsGrid.Columns.Add("Title", "Title");
            promotionsGrid.Columns.Add("Message", "Message");
            promotionsGrid.Columns.Add("Reason", "Reason");
            promotionsGrid.Columns.Add("SubmittedBy", "Submitted By");
            promotionsGrid.Columns.Add("Status", "Status");

            var approveColumn = new DataGridViewButtonColumn
            {
                Name = "Approve",
                HeaderText = "",
                Text = "✓ Approve",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat
            };
            promotionsGrid.Columns.Add(approveColumn);

            var rejectColumn = new DataGridViewButtonColumn
            {
                Name = "Reject",
                HeaderText = "",
                Text = "✕ Reject",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat
            };
            promotionsGrid.Columns.Add(rejectColumn);

            promotionsGrid.Columns["Title"].FillWeight = 100;
            promotionsGrid.Columns["Message"].FillWeight = 170;
            promotionsGrid.Columns["Reason"].FillWeight = 170;
            promotionsGrid.Columns["SubmittedBy"].FillWeight = 100;
            promotionsGrid.Columns["Status"].FillWeight = 100;
            promotionsGrid.Columns["Approve"].FillWeight = 100;
            promotionsGrid.Columns["Reject"].FillWeight = 100;

            foreach (DataGridViewColumn column in promotionsGrid.Columns)
            {
                if (column.CellTemplate is DataGridViewButtonCell) continue;
                column.ReadOnly = true;
                column.DefaultCellStyle.SelectionBackColor = Color.White;
                column.DefaultCellStyle.SelectionForeColor = Theme.TextPrimary;
            }
            promotionsGrid.Columns["Message"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            promotionsGrid.Columns["Reason"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            promotionsGrid.CellPainting += PromotionsGrid_CellPainting;
            promotionsGrid.CellClick += PromotionsGrid_CellClick;

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
            promotionsGrid.Rows.Add("No promotions submitted yet.", "", "", "", "", "", "");
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
                    "", "");

                promotionsGrid.Rows[rowIndex].Tag = promo;

                // Approve/Reject only make sense while it's still awaiting a decision.
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

        /// <summary>Draws the Status column as a colored pill, and tints the
        /// Approve/Reject buttons so they read as clear actions.</summary>
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

                var pillRect = Rectangle.Inflate(e.CellBounds, -12, -10);
                using (var path = RoundedRect.Path(pillRect, pillRect.Height / 2))
                using (var brush = new SolidBrush(colors.Bg))
                {
                    e.Graphics.FillPath(brush, path);
                }
                TextRenderer.DrawText(e.Graphics, status, StatusPillFont, pillRect, colors.Fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                e.Handled = true;
            }
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
