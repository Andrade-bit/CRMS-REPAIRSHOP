using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CRMS.winforms; // reuses RoundedPanel/RoundedButton from Login.cs
using CRMS.winforms.Shared;

namespace CRMS.Winforms.Admin
{
    /// <summary>
    /// Reusable drill-down popup behind a Dashboard KPI tile — shows the specific
    /// rows that make up that number. Data is passed in already-loaded (the
    /// Dashboard's own API call already fetched it), so opening this is instant,
    /// no extra request.
    /// </summary>
    public class KpiDetailDialog : Form
    {
        private static readonly Color CardColor = Color.White;
        private static readonly Color CardBorder = Color.FromArgb(232, 234, 238);
        private static readonly Color TitleColor = Color.FromArgb(23, 43, 66);
        private static readonly Color SubtleGray = Color.FromArgb(107, 128, 153);
        private static readonly Color Accent = Color.FromArgb(7, 107, 149);
        private static readonly Color AccentHover = Color.FromArgb(6, 88, 118);
        private static readonly Color PageBg = Color.FromArgb(248, 250, 252);
        private static readonly Color GridLine = Color.FromArgb(223, 230, 237);

        public KpiDetailDialog(string title, string subtitle, string[] columnHeaders, string[][] rows)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(760, 520);
            MinimumSize = new Size(560, 360);
            BackColor = PageBg;
            Font = new Font("Segoe UI", 9F);

            var header = new Panel { Dock = DockStyle.Top, Height = 76, BackColor = CardColor };
            header.Paint += (s, e) =>
            {
                using var pen = new Pen(CardBorder, 1);
                e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
            };

            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = TitleColor,
                AutoSize = true,
                Location = new Point(24, 14)
            };
            header.Controls.Add(titleLabel);

            header.Controls.Add(new Label
            {
                Text = subtitle,
                Font = new Font("Segoe UI", 10F),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(24, 14 + titleLabel.PreferredHeight + 4)
            });

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = CardColor };
            footer.Paint += (s, e) =>
            {
                using var pen = new Pen(CardBorder, 1);
                e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
            };
            var btnClose = new RoundedButton
            {
                Text = "Close",
                Size = new Size(110, 38),
                CornerRadius = 8,
                BackColor = Accent,
                HoverColor = AccentHover,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnClose.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            footer.Controls.Add(btnClose);
            footer.Resize += (s, e) => btnClose.Location = new Point(footer.Width - btnClose.Width - 24, (footer.Height - btnClose.Height) / 2);
            btnClose.Location = new Point(footer.Width - btnClose.Width - 24, (footer.Height - btnClose.Height) / 2);

            var bodyPanel = new Panel { Dock = DockStyle.Fill, BackColor = PageBg, Padding = new Padding(24, 18, 24, 18) };

            var tableCard = new RoundedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderColor = GridLine,
                CornerRadius = 12,
                Padding = new Padding(8)
            };

            if (rows.Length == 0)
            {
                var emptyLabel = new Label
                {
                    Text = "Nothing here right now.",
                    Font = new Font("Segoe UI", 11F, FontStyle.Italic),
                    ForeColor = SubtleGray,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                tableCard.Controls.Add(emptyLabel);
            }
            else
            {
                const int pageSize = 5;
                int page = 0;
                int totalPages = Math.Max(1, (int)Math.Ceiling(rows.Length / (double)pageSize));

                var grid = Theme.CreateStyledGrid();
                grid.ReadOnly = true;
                grid.RowTemplate.Height = 42;
                grid.CellBorderStyle = DataGridViewCellBorderStyle.Single;
                grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
                grid.GridColor = GridLine;

                foreach (var header2 in columnHeaders)
                    grid.Columns.Add(header2, header2);

                foreach (DataGridViewColumn column in grid.Columns)
                {
                    column.DefaultCellStyle.SelectionBackColor = Color.White;
                    column.DefaultCellStyle.SelectionForeColor = Theme.TextPrimary;
                }

                // Pager bar — only shown when there's more than one page, same
                // "hide entirely if everything fits" rule used everywhere else.
                var pager = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.White, Visible = totalPages > 1 };
                var btnPrev = new Button { Text = "‹ Prev", Width = 80, Height = 30, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
                btnPrev.FlatAppearance.BorderColor = GridLine;
                var btnNext = new Button { Text = "Next ›", Width = 80, Height = 30, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
                btnNext.FlatAppearance.BorderColor = GridLine;
                var pageIndicator = new Label { Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = SubtleGray, AutoSize = true };

                void RenderPage()
                {
                    grid.Rows.Clear();
                    foreach (var row in rows.Skip(page * pageSize).Take(pageSize))
                        grid.Rows.Add(row);

                    pageIndicator.Text = $"Page {page + 1} of {totalPages}";
                    btnPrev.Enabled = page > 0;
                    btnNext.Enabled = page < totalPages - 1;
                }

                void LayoutPager()
                {
                    const int gap = 24;
                    int groupWidth = btnPrev.Width + gap + pageIndicator.PreferredWidth + gap + btnNext.Width;
                    int startX = Math.Max(0, (pager.Width - groupWidth) / 2);
                    btnPrev.Location = new Point(startX, (pager.Height - btnPrev.Height) / 2);
                    pageIndicator.Location = new Point(btnPrev.Right + gap, (pager.Height - pageIndicator.Height) / 2);
                    btnNext.Location = new Point(pageIndicator.Right + gap, (pager.Height - btnNext.Height) / 2);
                }

                btnPrev.Click += (s, e) => { if (page > 0) { page--; RenderPage(); LayoutPager(); } };
                btnNext.Click += (s, e) => { if (page < totalPages - 1) { page++; RenderPage(); LayoutPager(); } };
                pager.Resize += (s, e) => LayoutPager();

                pager.Controls.Add(btnPrev);
                pager.Controls.Add(pageIndicator);
                pager.Controls.Add(btnNext);

                RenderPage();
                LayoutPager();

                // Fill (grid) added first, pager (Bottom) added after — same
                // reverse-add-order docking convention used throughout this app.
                tableCard.Controls.Add(grid);
                tableCard.Controls.Add(pager);
            }

            bodyPanel.Controls.Add(tableCard);

            Controls.Add(bodyPanel);
            Controls.Add(footer);
            Controls.Add(header);
        }
    }
}
