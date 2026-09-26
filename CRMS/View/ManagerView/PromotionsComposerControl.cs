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
using RoundedTextBox = CRMS.winforms.RoundedTextBox;
using RoundedMultilineBox = CRMS.winforms.Shared.RoundedMultilineBox;

namespace CRMS.winforms.ManagerView
{
    /// <summary>
    /// Manager's Promotions page: draft/submit a new offer, and see the current
    /// status of everything already submitted. Nothing here actually reaches a
    /// customer — Admin has to approve it first (AdminView's own Promotions page).
    /// </summary>
    public partial class PromotionsComposerControl : UserControl
    {
        private static readonly Color ManagerAccent = Color.FromArgb(10, 116, 163);
        private static readonly Color ManagerAccentHover = Color.FromArgb(8, 99, 140);
        private static readonly Color GridLine = Color.FromArgb(183, 228, 248);
        private static readonly Color FieldFill = Color.FromArgb(249, 249, 251);
        private static readonly Color CardBorder = Color.FromArgb(232, 234, 238);
        private static readonly Color TitleColor = Color.FromArgb(33, 37, 41);
        private static readonly Color SubtleGray = Color.FromArgb(130, 134, 145);

        private static readonly Dictionary<string, (Color Bg, Color Fg)> StatusPillColors = new()
        {
            [PromotionStatus.Draft] = (Color.FromArgb(236, 238, 241), Color.FromArgb(91, 107, 122)),
            [PromotionStatus.PendingApproval] = (Theme.StatusDiagnoseBg, Theme.StatusDiagnoseText),
            [PromotionStatus.Approved] = (Theme.StatusRepairedBg, Theme.StatusRepairedText),
            [PromotionStatus.Rejected] = (Color.FromArgb(251, 222, 222), Color.FromArgb(163, 45, 45)),
        };
        private static readonly Font StatusPillFont = new Font("Segoe UI", 8.75F, FontStyle.Bold);

        private readonly ManagerController _controller;

        private RoundedTextBox txtTitle = null!;
        private ComboBox cboType = null!;
        private RoundedMultilineBox txtMessage = null!;
        private RoundedMultilineBox txtReason = null!;
        private DataGridView promotionsGrid = null!;
        private Label toastLabel = null!;
        private System.Windows.Forms.Timer toastTimer = null!;

        public PromotionsComposerControl(ManagerController controller)
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
            promotionsGrid.RowTemplate.Height = 52;
            promotionsGrid.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            promotionsGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            promotionsGrid.GridColor = GridLine;

            promotionsGrid.Columns.Add("Title", "Title");
            promotionsGrid.Columns.Add("Message", "Message");
            promotionsGrid.Columns.Add("Reason", "Reason");
            promotionsGrid.Columns.Add("Type", "Type");
            promotionsGrid.Columns.Add("Status", "Status");
            promotionsGrid.Columns.Add("Submitted", "Submitted");

            promotionsGrid.Columns["Title"].FillWeight = 100;
            promotionsGrid.Columns["Message"].FillWeight = 170;
            promotionsGrid.Columns["Reason"].FillWeight = 170;
            promotionsGrid.Columns["Type"].FillWeight = 100;
            promotionsGrid.Columns["Status"].FillWeight = 100;
            promotionsGrid.Columns["Submitted"].FillWeight = 80;

            foreach (DataGridViewColumn column in promotionsGrid.Columns)
            {
                column.ReadOnly = true;
                column.DefaultCellStyle.SelectionBackColor = Color.White;
                column.DefaultCellStyle.SelectionForeColor = Theme.TextPrimary;
            }
            promotionsGrid.Columns["Message"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            promotionsGrid.Columns["Reason"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            promotionsGrid.CellPainting += PromotionsGrid_CellPainting;

            ShowEmptyState();
            tableCard.Controls.Add(promotionsGrid);

            var composerCard = BuildComposerCard();

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

            // Fill first, then Top pieces bottom-to-top (last added ends up highest).
            Controls.Add(tableCard);
            Controls.Add(toastLabel);
            Controls.Add(Theme.CreateSpacer(16));
            Controls.Add(composerCard);
            Controls.Add(Theme.CreateSpacer());
            Controls.Add(Theme.CreatePageHeader("Promotions", "Draft an offer for Admin to review — nothing sends until it's approved", showTitle: false, showSubtitle: false));

            ResumeLayout(false);
        }

        private RoundedPanel BuildComposerCard()
        {
            var card = new RoundedPanel
            {
                Dock = DockStyle.Top,
                Height = 356,
                BackColor = Color.White,
                BorderColor = CardBorder,
                CornerRadius = 14
            };

            const int pad = 20;

            card.Controls.Add(new Label
            {
                Text = "New Promotion",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = TitleColor,
                AutoSize = true,
                Location = new Point(pad, 16)
            });

            card.Controls.Add(new Label
            {
                Text = "TITLE",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(pad, 54)
            });
            txtTitle = new RoundedTextBox
            {
                Location = new Point(pad, 72),
                Size = new Size(340, 38),
                Placeholder = "e.g. Referral Rewards",
                BackColor = FieldFill,
                TextSize = 10F
            };
            card.Controls.Add(txtTitle);

            card.Controls.Add(new Label
            {
                Text = "TYPE",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(pad + 356, 54)
            });
            cboType = new ComboBox
            {
                Location = new Point(pad + 356, 72),
                Size = new Size(180, 38),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F)
            };
            cboType.Items.AddRange(PromotionType.All);
            cboType.SelectedIndex = 0;
            card.Controls.Add(cboType);

            card.Controls.Add(new Label
            {
                Text = "MESSAGE TO CUSTOMERS",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(pad, 122)
            });
            txtMessage = new RoundedMultilineBox
            {
                Location = new Point(pad, 140),
                Size = new Size(536, 60),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Placeholder = "The offer text a customer would actually see...",
                BackColor = FieldFill,
                TextSize = 10F
            };
            card.Controls.Add(txtMessage);

            card.Controls.Add(new Label
            {
                Text = "REASON (shown to Admin — why should this be approved?)",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(pad, 212)
            });
            txtReason = new RoundedMultilineBox
            {
                Location = new Point(pad, 230),
                Size = new Size(536, 60),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Placeholder = "e.g. Clearing out slow-moving stock, matching a competitor's offer, rewarding repeat customers...",
                BackColor = FieldFill,
                TextSize = 10F
            };
            card.Controls.Add(txtReason);

            var btnDraft = new CRMS.winforms.RoundedButton
            {
                Text = "Save as Draft",
                Location = new Point(pad, 306),
                Size = new Size(140, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                BackColor = Color.White,
                HoverColor = Color.FromArgb(245, 246, 248),
                ForeColor = TitleColor,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            btnDraft.Click += (s, e) => _ = SubmitAsync(submitForApproval: false);
            card.Controls.Add(btnDraft);

            var btnSubmit = new CRMS.winforms.RoundedButton
            {
                Text = "Submit for Approval",
                Location = new Point(pad + 152, 306),
                Size = new Size(180, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                BackColor = ManagerAccent,
                HoverColor = ManagerAccentHover,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            btnSubmit.Click += (s, e) => _ = SubmitAsync(submitForApproval: true);
            card.Controls.Add(btnSubmit);

            return card;
        }

        private async Task SubmitAsync(bool submitForApproval)
        {
            string title = txtTitle.Value.Trim();
            string message = txtMessage.Value.Trim();
            string reason = txtReason.Value.Trim();
            string type = cboType.SelectedItem?.ToString() ?? PromotionType.General;

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
            {
                MessageBox.Show(FindForm(), "Please fill in a title and a message.",
                    "Missing Details", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // A draft can stay unexplained while it's still being worked on, but
            // Admin needs to know why they're being asked to approve this.
            if (submitForApproval && string.IsNullOrWhiteSpace(reason))
            {
                MessageBox.Show(FindForm(), "Please explain why this promotion should be approved.",
                    "Missing Reason", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var (success, msg, _) = await _controller.CreatePromotionAsync(title, type, message, reason, submitForApproval);
            if (!success)
            {
                MessageBox.Show(FindForm(), $"Could not save the promotion:\n{msg}",
                    "Save Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            txtTitle.Value = string.Empty;
            txtMessage.Value = string.Empty;
            txtReason.Value = string.Empty;
            cboType.SelectedIndex = 0;

            await LoadData();
            ShowToast(submitForApproval
                ? $"✓ \"{title}\" submitted for Admin's approval."
                : $"✓ \"{title}\" saved as a draft.");
        }

        private void ShowEmptyState()
        {
            promotionsGrid.Rows.Add("No promotions yet.", "", "", "", "", "");
            promotionsGrid.Rows[0].ReadOnly = true;
            promotionsGrid.Rows[0].DefaultCellStyle = new DataGridViewCellStyle
            {
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 9.75F, FontStyle.Italic)
            };
        }

        /// <summary>Reloads this Manager's own promotions. Call on every page navigation.</summary>
        public async Task LoadData()
        {
            promotionsGrid.Rows.Clear();

            var promotions = await _controller.GetMyPromotionsAsync();

            if (promotions.Count == 0)
            {
                ShowEmptyState();
                return;
            }

            foreach (var promo in promotions)
            {
                promotionsGrid.Rows.Add(
                    promo.Title,
                    promo.Message,
                    string.IsNullOrWhiteSpace(promo.Reason) ? "—" : promo.Reason,
                    promo.Type,
                    promo.Status == PromotionStatus.PendingApproval ? "Pending Approval" : promo.Status,
                    promo.CreatedAt.ToString("MMM d, yyyy"));
            }
        }

        /// <summary>Draws the Status column as a colored pill, matching the pattern
        /// used for ticket status pills elsewhere in the app.</summary>
        private void PromotionsGrid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (promotionsGrid.Columns[e.ColumnIndex].Name != "Status") return;

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
