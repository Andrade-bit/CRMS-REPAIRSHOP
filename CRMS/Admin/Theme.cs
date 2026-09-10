using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CRMS.Winforms.Admin
{
    /// <summary>
    /// Shared colors, typography and layout helpers used by every admin page,
    /// so headers/cards/spacing stay identical across the app instead of being
    /// redeclared (and drifting) in each control.
    /// </summary>
    internal static class Theme
    {
        // Brand / accent
        public static readonly Color Accent = Color.FromArgb(86, 182, 255);
        public static readonly Color AccentHover = Color.FromArgb(66, 162, 235);
        public static readonly Color AccentPressed = Color.FromArgb(46, 142, 215);

        // Sidebar
        public static readonly Color SidebarBg = Accent;
        public static readonly Color SidebarHover = AccentHover;
        public static readonly Color SidebarPressed = AccentPressed;
        public static readonly Color SidebarText = Color.White;
        public static readonly Color SidebarLabel = Color.FromArgb(219, 238, 255);
        public static readonly Color SidebarActiveText = Color.FromArgb(30, 96, 145);

        // Page
        public static readonly Color PageBg = Color.White;
        public static readonly Color TextPrimary = Color.FromArgb(28, 28, 30);
        public static readonly Color TextSecondary = Color.FromArgb(117, 117, 117);
        public static readonly Color BorderColor = Color.FromArgb(235, 235, 240);
        public static readonly Color HeaderBg = Color.FromArgb(250, 250, 252);
        public static readonly Color RowAlt = Color.FromArgb(248, 249, 251);
        public static readonly Color SelectionTint = Color.FromArgb(244, 249, 255);

        // Typography
        public static readonly Font FontPageTitle = new Font("Segoe UI", 20F, FontStyle.Bold);
        public static readonly Font FontPageSubtitle = new Font("Segoe UI", 10F, FontStyle.Regular);
        public static readonly Font FontNav = new Font("Segoe UI", 10F, FontStyle.Regular);
        public static readonly Font FontNavActive = new Font("Segoe UI", 10F, FontStyle.Bold);
        public static readonly Font FontBody = new Font("Segoe UI", 10F, FontStyle.Regular);

        // Layout
        public const int PageMarginX = 40;
        public const int PageMarginTop = 32;

        public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

        /// <summary>Clips a control to a rounded rectangle (buttons, chips, etc).</summary>
        public static void ApplyRoundedRegion(Control control, int radius)
        {
            control.Region = Region.FromHrgn(
                CreateRoundRectRgn(0, 0, control.Width, control.Height, radius, radius));
        }

        /// <summary>
        /// Standard page header: big title + secondary subtitle, with an optional
        /// right-aligned action control (e.g. a "Create" button) that stays pinned
        /// to the top-right as the page resizes.
        /// </summary>
        public static Panel CreatePageHeader(string title, string subtitle, Control action = null)
        {
            // Measure the title's actual rendered height instead of hardcoding a
            // Y offset for the subtitle — this is what was causing the overlap
            // (20pt bold text is taller than the old fixed 38px gap assumed).
            Size titleSize = TextRenderer.MeasureText(title, FontPageTitle);
            Size subtitleSize = TextRenderer.MeasureText(subtitle, FontPageSubtitle);

            const int topPad = 4;
            const int gapBetween = 6;   // space between title baseline and subtitle
            const int bottomPad = 10;   // space below subtitle before next control

            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = topPad + titleSize.Height + gapBetween + subtitleSize.Height + bottomPad,
                BackColor = PageBg
            };

            var titleLabel = new Label
            {
                Text = title,
                Font = FontPageTitle,
                ForeColor = TextPrimary,
                AutoSize = true,
                Location = new Point(0, topPad)
            };

            var subtitleLabel = new Label
            {
                Text = subtitle,
                Font = FontPageSubtitle,
                ForeColor = TextSecondary,
                AutoSize = true,
                Location = new Point(0, topPad + titleSize.Height + gapBetween)
            };

            panel.Controls.Add(titleLabel);
            panel.Controls.Add(subtitleLabel);

            if (action != null)
            {
                action.Anchor = AnchorStyles.Top | AnchorStyles.Right;

                void Reposition()
                {
                    action.Location = new Point(
                        panel.Width - action.Width - 0,
                        (panel.Height - action.Height) / 2);
                }

                panel.Resize += (s, e) => Reposition();
                panel.Controls.Add(action);
                Reposition();
            }

            return panel;
        }

        /// <summary>A thin top spacer, used to keep vertical rhythm identical between pages.</summary>
        public static Panel CreateSpacer(int height = 24)
        {
            return new Panel { Dock = DockStyle.Top, Height = height, BackColor = PageBg };
        }

        /// <summary>Bordered fill card used for "coming soon" / empty states, matching the table card look.</summary>
        public static Panel CreatePlaceholderCard(string message)
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = PageBg };

            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(BorderColor, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                }
            };

            var label = new Label
            {
                Text = message,
                Font = FontBody,
                ForeColor = TextSecondary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            card.Controls.Add(label);
            return card;
        }
    }
}