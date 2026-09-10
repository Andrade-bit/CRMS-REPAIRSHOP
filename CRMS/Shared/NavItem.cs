using System;
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
        private readonly Label textLabel;
        private const int Inset = 14;

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
                BackColor = Theme.SidebarBg,
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
            Color labelBg = active ? Color.White : Theme.SidebarBg;
            textLabel.BackColor = labelBg;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var bounds = new Rectangle(Inset, 2, Math.Max(0, Width - Inset * 2), Math.Max(0, Height - 4));

            Color? pillColor = null;
            if (active) pillColor = Color.White;
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

        public void SetActive(bool isActive)
        {
            active = isActive;

            textLabel.ForeColor = active ? Theme.SidebarActiveText : Theme.SidebarText;
            textLabel.Font = active ? Theme.FontNavActive : Theme.FontNav;
            textLabel.BackColor = active ? Color.White : Theme.SidebarBg;

            Invalidate();
        }
    }
}
