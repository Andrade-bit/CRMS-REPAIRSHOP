using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace CRMS.winforms.Shared
{
    /// <summary>
    /// A single sidebar navigation row (icon-less pill that highlights white when
    /// active). Shared by every role's shell (Admin, Staff, Manager, Superadmin)
    /// so the sidebar looks and behaves identically everywhere.
    /// </summary>
    public sealed class NavItem : Panel
    {
        private bool active;
        private bool hover;
        private bool pressed;
        private bool transparentMode;
        private readonly Label textLabel;
        private const int Inset = 14;

        // Opt-in: when set, this item paints no flat background of its own (so
        // it can sit on a custom-painted/gradient sidebar) and uses translucent
        // white overlays for hover/pressed instead of Theme's flat sidebar
        // tints. Every existing shell (Admin, Manager, Superadmin) leaves this
        // false and renders exactly as before.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool TransparentMode
        {
            get => transparentMode;
            set
            {
                transparentMode = value;
                if (value)
                {
                    SetStyle(ControlStyles.SupportsTransparentBackColor, true);
                    BackColor = Color.Transparent;
                }
            }
        }

        public NavItem(string text)
        {
            DoubleBuffered = true;
            BackColor = Theme.SidebarBg;
            Cursor = Cursors.Hand;
            Padding = new Padding(Inset + 14, 0, Inset, 0);

            textLabel = new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Theme.SidebarText,
                Font = Theme.FontNav,
                // Always transparent, never an opaque fill: the label is docked
                // to this panel's full (rectangular) bounds, but the active/
                // hover pill drawn in OnPaint below is rounded — an opaque
                // label would square off the pill's right-side corners where
                // their edges coincide.
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };

            Controls.Add(textLabel);

            textLabel.Click += (s, e) => base.OnClick(EventArgs.Empty);

            MouseEnter += (s, e) => SetHover(true);
            MouseLeave += (s, e) => SetHover(false);
            textLabel.MouseEnter += (s, e) => SetHover(true);
            textLabel.MouseLeave += (s, e) => SetHover(false);

            MouseDown += (s, e) => { pressed = true; Invalidate(); };
            MouseUp += (s, e) => { pressed = false; Invalidate(); };
            textLabel.MouseDown += (s, e) => { pressed = true; Invalidate(); };
            textLabel.MouseUp += (s, e) => { pressed = false; Invalidate(); };
        }

        private void SetHover(bool value)
        {
            hover = value;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var bounds = new Rectangle(Inset, 2, Math.Max(0, Width - Inset * 2), Math.Max(0, Height - 4));

            Color? pillColor = null;
            if (active) pillColor = Color.White;
            else if (transparentMode)
            {
                if (pressed) pillColor = Color.FromArgb(30, 255, 255, 255);
                else if (hover) pillColor = Color.FromArgb(18, 255, 255, 255);
            }
            else if (pressed) pillColor = Theme.SidebarPressed;
            else if (hover) pillColor = Theme.SidebarHover;

            if (pillColor.HasValue && bounds.Width > 0 && bounds.Height > 0)
            {
                using (var path = Theme.RoundedRect(bounds, 10))
                using (var brush = new SolidBrush(pillColor.Value))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }

            base.OnPaint(e);
        }

        /// <summary>Changes the row's label after construction — used by a
        /// collapsible parent item (e.g. "Reports") to swap its disclosure
        /// arrow between collapsed/expanded without rebuilding the control.</summary>
        public void SetText(string text) => textLabel.Text = text;

        public void SetActive(bool isActive)
        {
            active = isActive;

            textLabel.ForeColor = active ? Theme.SidebarActiveText : Theme.SidebarText;
            textLabel.Font = active ? Theme.FontNavActive : Theme.FontNav;

            Invalidate();
        }
    }
}
