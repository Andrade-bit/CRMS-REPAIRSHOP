using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS.winforms;          // reuses RoundedButton/RoundedPanel from Login.cs
using CRMS.winforms.Api;

namespace CRMS.winforms.StaffView
{
    public class StaffForm : Form
    {
        private readonly LoginResult _session;

        // ----- Colors -----
        private static readonly Color BgColor = Color.FromArgb(237, 239, 242);
        private static readonly Color SidebarColor = Color.White;
        private static readonly Color TitleColor = Color.FromArgb(33, 37, 41);
        private static readonly Color SubtleGray = Color.FromArgb(140, 144, 153);
        private static readonly Color Accent = Color.FromArgb(58, 150, 150);
        private static readonly Color AccentHover = Color.FromArgb(47, 130, 130);
        private static readonly Color PillBorder = Color.FromArgb(222, 224, 230);
        private static readonly Color TableHeaderGray = Color.FromArgb(150, 153, 163);

        public StaffForm(LoginResult session)
        {
            _session = session;
            BuildLayout();
        }

        private void BuildLayout()
        {
            Text = "Staff Dashboard";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1000, 640);
            MinimumSize = new Size(900, 560);
            BackColor = BgColor;
            Font = new Font("Segoe UI", 9F);

            // ===================== Sidebar =====================
            var sidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 90,
                BackColor = SidebarColor
            };
            Controls.Add(sidebar);

            // Logo / brand icon
            var logoCircle = new Panel
            {
                Size = new Size(44, 44),
                Location = new Point(23, 24),
                BackColor = Accent
            };
            MakeRound(logoCircle);
            sidebar.Controls.Add(logoCircle);

            var logoIcon = new Label
            {
                Text = "\uD83D\uDD27", // wrench emoji as a placeholder icon
                Font = new Font("Segoe UI Emoji", 14F),
                ForeColor = Color.White,
                AutoSize = false,
                Size = new Size(44, 44),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            logoCircle.Controls.Add(logoIcon);

            var brandLabel = new Label
            {
                Text = "Fix",
                Font = new Font("Segoe UI", 7.5F),
                ForeColor = SubtleGray,
                AutoSize = true
            };
            brandLabel.Location = new Point((90 - brandLabel.PreferredWidth) / 2, 72);
            sidebar.Controls.Add(brandLabel);

            // Nav items
            AddNavItem(sidebar, "\uD83C\uDFAB", "Tickets", 140, selected: true);
            AddNavItem(sidebar, "\uD83D\uDD59", "History", 210, selected: false);

            // Log out, pinned to bottom
            var logoutIcon = new Label
            {
                Text = "\u21AA",
                Font = new Font("Segoe UI", 13F),
                ForeColor = SubtleGray,
                AutoSize = false,
                Size = new Size(90, 24),
                TextAlign = ContentAlignment.MiddleCenter
            };
            logoutIcon.Location = new Point(0, ClientSize.Height - 70);
            logoutIcon.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            sidebar.Controls.Add(logoutIcon);

            var logoutLabel = new Label
            {
                Text = "Log out",
                Font = new Font("Segoe UI", 7.5F),
                ForeColor = SubtleGray,
                AutoSize = false,
                Size = new Size(90, 16),
                TextAlign = ContentAlignment.MiddleCenter
            };
            logoutLabel.Location = new Point(0, ClientSize.Height - 46);
            logoutLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            sidebar.Controls.Add(logoutLabel);

            // ===================== Main content =====================
            var content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgColor,
                Padding = new Padding(40, 30, 40, 30)
            };
            Controls.Add(content);
            content.BringToFront();

            // --- Header row ---
            var titleLabel = new Label
            {
                Text = "Service Tickets",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = TitleColor,
                AutoSize = true,
                Location = new Point(40, 24)
            };
            content.Controls.Add(titleLabel);

            var dateLabel = new Label
            {
                Text = DateTime.Now.ToString("dddd, MMMM d, yyyy"),
                Font = new Font("Segoe UI", 9F),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(40, 52)
            };
            content.Controls.Add(dateLabel);

            // Notification bell (top-right)
            var bellPanel = new Panel
            {
                Size = new Size(36, 36),
                BackColor = Color.White
            };
            MakeRound(bellPanel);
            content.Controls.Add(bellPanel);

            var bellIcon = new Label
            {
                Text = "\uD83D\uDD14",
                Font = new Font("Segoe UI Emoji", 10F),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(36, 36),
                BackColor = Color.Transparent
            };
            bellPanel.Controls.Add(bellIcon);

            var badge = new Label
            {
                Text = "2",
                Font = new Font("Segoe UI", 7F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Accent,
                Size = new Size(16, 16),
                TextAlign = ContentAlignment.MiddleCenter
            };
            MakeRound(badge);
            content.Controls.Add(badge);
            badge.BringToFront();

            // "+ New Service Request" button
            var newRequestBtn = new RoundedButton
            {
                Text = "+  New Service Request",
                Size = new Size(190, 36),
                BackColor = Accent,
                HoverColor = AccentHover,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            content.Controls.Add(newRequestBtn);

            // Position the top-right cluster (bell + badge + button) relative to content width.
            // Deliberately no Anchor on these controls — WinForms' automatic anchor
            // repositioning would run alongside this manual code and fight it,
            // which is what caused the button to drift and overlap the title.
            void PositionHeaderRight()
            {
                int right = content.ClientSize.Width - 20;
                newRequestBtn.Location = new Point(right - newRequestBtn.Width, 20);
                bellPanel.Location = new Point(newRequestBtn.Left - 46, 20);
                badge.Location = new Point(bellPanel.Right - 12, bellPanel.Top - 4);
            }
            content.Resize += (s, e) => PositionHeaderRight();
            Load += (s, e) => PositionHeaderRight();
            PositionHeaderRight();

            // --- Filter pills ---
            var filters = new[] { "All", "Received", "Diagnosing", "Waiting for Parts", "In Repair", "Ready for Pickup" };
            int fx = 40;
            const int fy = 90;
            for (int i = 0; i < filters.Length; i++)
            {
                bool selected = i == 0;
                var pill = new RoundedButton
                {
                    Text = filters[i],
                    Font = new Font("Segoe UI", 8.5F, selected ? FontStyle.Bold : FontStyle.Regular),
                    AutoSize = false,
                    Height = 30,
                    BackColor = selected ? Accent : Color.White,
                    HoverColor = selected ? AccentHover : Color.FromArgb(245, 246, 248),
                    ForeColor = selected ? Color.White : TitleColor,
                    CornerRadius = 15
                };
                int textWidth = TextRenderer.MeasureText(pill.Text, pill.Font).Width;
                pill.Width = textWidth + 32;
                pill.Location = new Point(fx, fy);
                content.Controls.Add(pill);
                fx += pill.Width + 10;
            }

            // --- Table header row ---
            const int tableY = 140;
            var colTicket = MakeTableHeader("TICKET ID", 40, tableY);
            var colCustomer = MakeTableHeader("CUSTOMER / DEVICE", 190, tableY);
            var colStatus = MakeTableHeader("STATUS", 500, tableY);
            var colPayment = MakeTableHeader("PAYMENT", 660, tableY);
            var colDate = MakeTableHeader("DATE", 820, tableY);
            foreach (var lbl in new[] { colTicket, colCustomer, colStatus, colPayment, colDate })
                content.Controls.Add(lbl);

            var headerRule = new Panel
            {
                Height = 1,
                BackColor = PillBorder,
                Location = new Point(40, tableY + 22),
                Width = content.ClientSize.Width - 80
            };
            content.Controls.Add(headerRule);
            content.Resize += (s, e) => headerRule.Width = content.ClientSize.Width - 80;

            // Table body intentionally left empty — no data/functionality yet.
        }

        private Label MakeTableHeader(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = TableHeaderGray,
                AutoSize = true,
                Location = new Point(x, y)
            };
        }

        private void AddNavItem(Panel sidebar, string icon, string label, int y, bool selected)
        {
            var iconLabel = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI Emoji", 13F),
                ForeColor = selected ? Accent : SubtleGray,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(90, 24),
                Location = new Point(0, y)
            };
            sidebar.Controls.Add(iconLabel);

            var textLabel = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 7.5F, selected ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = selected ? Accent : SubtleGray,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(90, 16),
                Location = new Point(0, y + 24)
            };
            sidebar.Controls.Add(textLabel);
        }

        private static void MakeRound(Control control)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(0, 0, control.Width, control.Height);
            control.Region = new Region(path);
        }
    }
}