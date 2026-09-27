using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CRMS.winforms.Shared
{
    public enum CrmsRole
    {
        Superadmin,
        Admin,
        Manager,
        Staff
    }

    public sealed class CrmsNavigationEventArgs : EventArgs
    {
        public string PageKey { get; }

        public CrmsNavigationEventArgs(string pageKey)
        {
            PageKey = pageKey;
        }
    }

    /// <summary>
    /// Shared header + sidebar shell used by all four roles' MainForms — each
    /// MainForm still owns its own page controls and its own NavigateTo/
    /// SwitchContent logic; this only replaces the hand-built header/sidebar
    /// visuals, and forwards clicks back via PageRequested.
    /// </summary>
    public sealed class ModernCrmsLayout : UserControl
    {
        private readonly Color sidebarColor;
        private readonly Color selectedColor;
        private readonly Color hoverColor;

        private readonly Panel sidebar;
        private readonly Panel header;
        private readonly Label pageTitle;
        private readonly FlowLayoutPanel headerExtras;
        private readonly FlowLayoutPanel navigation;
        private readonly List<SidebarButton> menuButtons = new();
        private readonly List<MenuGroup> menuGroups = new();

        // Add your existing UserControls to this panel.
        public Panel ContentPanel { get; }

        /// <summary>The header bar itself — for adding something like a sync
        /// status indicator, positioned to sit just left of the 244px-wide
        /// profile block docked to the header's right edge.</summary>
        public Panel Header => header;

        /// <summary>The profile block's fixed width, for positioning anything
        /// added to Header relative to it.</summary>
        public const int ProfileWidth = 244;

        /// <summary>The header bar's fixed height — never changes at runtime,
        /// so anything centering itself vertically in the header (or in
        /// HeaderExtras) can use this directly instead of reading Header.Height,
        /// which isn't reliably resolved yet the first time a MainForm builds
        /// its header content.</summary>
        public const int HeaderHeight = 80;

        /// <summary>Adds a control to the header, flush against the left edge of
        /// the profile block, vertically centered — e.g. a sync status indicator
        /// or a small search box. Dock-managed (not manually positioned from
        /// Header.Width), so it stays correctly placed through every real
        /// layout pass without a MainForm needing its own Resize plumbing.</summary>
        public void AddHeaderExtra(Control control)
        {
            control.Margin = new Padding(0, Math.Max(0, (HeaderHeight - control.Height) / 2), 12, 0);
            headerExtras.Controls.Add(control);
        }

        public event EventHandler<CrmsNavigationEventArgs>? PageRequested;
        public event EventHandler? LogoutRequested;

        public ModernCrmsLayout(CrmsRole role, string fullName)
        {
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;

            Dock = DockStyle.Fill;
            Font = new Font("Segoe UI", 9.5F);
            BackColor = Color.FromArgb(245, 248, 252);

            sidebarColor = role switch
            {
                CrmsRole.Superadmin => Color.FromArgb(18, 53, 79),
                CrmsRole.Admin => Color.FromArgb(12, 102, 137),
                CrmsRole.Manager => Color.FromArgb(10, 116, 163),
                _ => Color.FromArgb(8, 126, 180)
            };

            selectedColor = Blend(sidebarColor, Color.White, 0.16F);
            hoverColor = Blend(sidebarColor, Color.White, 0.08F);

            SuspendLayout();

            // RIGHT SIDE: header and page content.
            var workspace = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BackColor
            };

            ContentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BackColor,
                Padding = Padding.Empty
            };

            header = new Panel
            {
                Dock = DockStyle.Top,
                Height = HeaderHeight,
                BackColor = Color.White,
                Padding = new Padding(26, 0, 24, 0)
            };

            header.Paint += (_, e) =>
            {
                using var pen = new Pen(Color.FromArgb(228, 234, 241));

                e.Graphics.DrawLine(
                    pen,
                    0,
                    header.Height - 1,
                    header.Width,
                    header.Height - 1);
            };

            pageTitle = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Dashboard",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 38, 58),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                UseMnemonic = false
            };

            Panel profile = BuildProfile(fullName, role.ToString());

            // Dock-managed, not manually positioned from Header.Width like the
            // old sync-indicator code was — that relied on reading Header.Width
            // at the right moment and never reliably got a second chance to
            // correct itself once real layout landed, which is why the sync
            // indicator silently never showed up at all. Docking fixes this
            // structurally: WinForms itself keeps headerExtras flush against
            // profile's left edge on every real layout pass, no matter when.
            headerExtras = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.White,
                Margin = Padding.Empty,
                Padding = new Padding(0, 0, 16, 0)
            };

            header.Controls.Add(pageTitle);
            header.Controls.Add(headerExtras);
            header.Controls.Add(profile);

            workspace.Controls.Add(ContentPanel);
            workspace.Controls.Add(header);

            // LEFT SIDE: branding, menu, logout.
            sidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 260,
                BackColor = sidebarColor
            };

            Panel brand = BuildBrand(role);

            navigation = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = sidebarColor,
                Padding = new Padding(12, 8, 12, 8)
            };

            Panel logoutArea = BuildLogout();

            sidebar.Controls.Add(navigation);
            sidebar.Controls.Add(logoutArea);
            sidebar.Controls.Add(brand);

            Controls.Add(workspace);
            Controls.Add(sidebar);

            BuildMenu(role);

            navigation.ClientSizeChanged += (_, _) => ResizeMenu();
            ResizeMenu();

            if (menuButtons.Count > 0)
                SetActivePage(menuButtons[0].PageKey);

            ResumeLayout(true);
        }

        private Panel BuildBrand(CrmsRole role)
        {
            var brand = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                BackColor = sidebarColor
            };

            var logo = new PictureBox
            {
                Image = IconResources.Get("logo"),
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(18, 24),
                Size = new Size(48, 48),
                BackColor = Color.Transparent
            };

            var nameFont = new Font("Segoe UI", 23F, FontStyle.Bold);
            var name = new Label
            {
                Text = "CRMS",
                Font = nameFont,
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(80, 24)
            };

            // Measured instead of a fixed Y offset — the hardcoded 68 assumed
            // a shorter rendered height for "CRMS" at 23pt bold than this font
            // actually produces, which is what was overlapping the role label
            // underneath it.
            int nameHeight = TextRenderer.MeasureText("CRMS", nameFont).Height;

            var roleLabel = new Label
            {
                Text = role.ToString().ToUpperInvariant(),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(195, 225, 241),
                AutoSize = true,
                Location = new Point(83, 24 + nameHeight + 4)
            };

            brand.Controls.Add(logo);
            brand.Controls.Add(name);
            brand.Controls.Add(roleLabel);

            return brand;
        }

        private Panel BuildProfile(string fullName, string role)
        {
            fullName = string.IsNullOrWhiteSpace(fullName)
                ? "User"
                : fullName.Trim();

            var profile = new Panel
            {
                Dock = DockStyle.Right,
                Width = 244,
                BackColor = Color.White
            };

            var avatar = new InitialsAvatar
            {
                Initials = GetInitials(fullName),
                AccentColor = sidebarColor,
                Location = new Point(0, 18),
                Size = new Size(44, 44)
            };

            var nameLabel = new Label
            {
                Text = fullName,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 38, 58),
                Location = new Point(56, 20),
                Size = new Size(188, 23),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                UseMnemonic = false
            };

            var roleLabel = new Label
            {
                Text = role,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(112, 126, 145),
                Location = new Point(56, 43),
                Size = new Size(188, 20)
            };

            profile.Controls.Add(avatar);
            profile.Controls.Add(nameLabel);
            profile.Controls.Add(roleLabel);

            return profile;
        }

        private Panel BuildLogout()
        {
            var area = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 76,
                BackColor = sidebarColor,
                Padding = new Padding(12, 14, 12, 14)
            };

            area.Paint += (_, e) =>
            {
                using var pen = new Pen(
                    Blend(sidebarColor, Color.White, 0.18F));

                e.Graphics.DrawLine(pen, 20, 0, area.Width - 20, 0);
            };

            var logout = CreateButton("Logout", "", "Logout");
            logout.Dock = DockStyle.Fill;
            logout.Click += (_, _) => LogoutRequested?.Invoke(this, EventArgs.Empty);

            area.Controls.Add(logout);

            return area;
        }

        private void BuildMenu(CrmsRole role)
        {
            switch (role)
            {
                case CrmsRole.Superadmin:
                    AddMenu("Dashboard", "", "Dashboard");
                    AddMenu("Subscriptions", "", "Subscriptions");
                    AddMenu("Platform Reports", "", "Reports");
                    AddMenu("Terms & Agreements", "", "Terms");
                    // pageKey "Users" (not "Companies") to match
                    // SuperAdminMainForm's existing SwitchContent cases.
                    AddMenu("Company Management", "", "Users");
                    break;

                case CrmsRole.Admin:
                    AddMenu("Dashboard", "", "Dashboard");
                    AddMenu("User Management", "", "Users");
                    AddMenu("Customer History", "", "History");
                    AddMenu("Promotions", "", "Promotions");
                    AddMenuGroup("Reports", "", "Reports", new[]
                    {
                        ("Monthly Sales", "Reports.Sales"),
                        ("Top Customers", "Reports.Customers"),
                        ("Common Repairs", "Reports.Common"),
                        ("Customer Feedback", "Reports.Feedback"),
                        ("Promotion Effectiveness", "Reports.Promotions"),
                    });
                    AddMenu("Branch Management", "", "Branches");
                    AddMenu("Terms & Conditions", "", "Terms");
                    break;

                case CrmsRole.Manager:
                    AddMenu("Dashboard", "", "Dashboard");
                    AddMenu("Pending Customers", "", "Pending");
                    AddMenu("Customer History", "", "History");
                    AddMenu("Promotions", "", "Promotions");
                    break;

                case CrmsRole.Staff:
                    AddMenu("Pending Customers", "", "Pending");
                    AddMenu("Customer History", "", "History");
                    break;
            }
        }

        private void AddMenu(string text, string icon, string pageKey)
        {
            SidebarButton button = CreateButton(text, icon, pageKey);

            button.Click += (_, _) =>
            {
                SetActivePage(pageKey);

                PageRequested?.Invoke(
                    this,
                    new CrmsNavigationEventArgs(pageKey));
            };

            menuButtons.Add(button);
            navigation.Controls.Add(button);
        }

        /// <summary>Adds a parent item that expands into an indented list of
        /// child pages instead of navigating directly — the first expand also
        /// requests the first child's page, matching a normal accordion menu.
        /// Height-animates open/closed the same way the old Reports submenu did.</summary>
        private void AddMenuGroup(string text, string icon, string groupKey, (string Text, string PageKey)[] children)
        {
            var parent = CreateButton(text, icon, groupKey);
            parent.IsGroupHeader = true;

            var childContainer = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = false,
                Height = 0,
                Width = 236,
                Margin = new Padding(0),
                BackColor = sidebarColor
            };

            var group = new MenuGroup { GroupKey = groupKey, Parent = parent, ChildContainer = childContainer };

            foreach (var (childText, childKey) in children)
            {
                var childButton = CreateButton(childText, icon, childKey);
                childButton.IsChild = true;
                childButton.Height = 40;
                childButton.Click += (_, _) =>
                {
                    SetActivePage(childKey);
                    PageRequested?.Invoke(this, new CrmsNavigationEventArgs(childKey));
                };
                menuButtons.Add(childButton);
                group.Children.Add(childButton);
                childContainer.Controls.Add(childButton);
            }

            var animTimer = new System.Windows.Forms.Timer { Interval = 12 };
            int targetHeight = 0;
            // Each child is 40px tall but CreateButton also gives it a 6px
            // bottom margin (same as every other menu button) — leaving that
            // out of the target height is what was clipping the last child
            // against whatever sits below the group in the sidebar.
            int fullHeight = children.Length * (40 + 6);
            animTimer.Tick += (_, _) =>
            {
                const int step = 24;
                if (childContainer.Height < targetHeight)
                    childContainer.Height = Math.Min(targetHeight, childContainer.Height + step);
                else if (childContainer.Height > targetHeight)
                    childContainer.Height = Math.Max(targetHeight, childContainer.Height - step);
                else
                    animTimer.Stop();
            };
            group.AnimTimer = animTimer;
            group.FullHeight = fullHeight;

            parent.Click += (_, _) =>
            {
                group.Expanded = !group.Expanded;
                parent.Expanded = group.Expanded;
                parent.Invalidate();
                targetHeight = group.Expanded ? fullHeight : 0;
                animTimer.Start();

                if (group.Expanded && group.Children.Count > 0)
                {
                    string firstKey = group.Children[0].PageKey;
                    SetActivePage(firstKey);
                    PageRequested?.Invoke(this, new CrmsNavigationEventArgs(firstKey));
                }
            };

            menuButtons.Add(parent);
            menuGroups.Add(group);
            navigation.Controls.Add(parent);
            navigation.Controls.Add(childContainer);
        }

        private SidebarButton CreateButton(
            string text,
            string icon,
            string pageKey)
        {
            return new SidebarButton
            {
                Text = text,
                IconGlyph = icon,
                PageKey = pageKey,
                Width = 236,
                Height = 48,
                Margin = new Padding(0, 0, 0, 6),
                BackColor = sidebarColor,
                SelectedColor = selectedColor,
                HoverColor = hoverColor
            };
        }

        private void ResizeMenu()
        {
            int availableWidth =
                navigation.ClientSize.Width -
                navigation.Padding.Horizontal;

            if (navigation.VerticalScroll.Visible)
                availableWidth -= SystemInformation.VerticalScrollBarWidth;

            int w = Math.Max(100, availableWidth);
            foreach (SidebarButton button in menuButtons)
                button.Width = w;
            foreach (var group in menuGroups)
                group.ChildContainer.Width = w;
        }

        /// <summary>Shows or hides a top-level menu item (by its pageKey) —
        /// FlowLayoutPanel reflows the rest of the menu automatically, so
        /// hiding one just closes the gap instead of leaving dead space.</summary>
        public void SetItemVisible(string pageKey, bool visible)
        {
            foreach (SidebarButton button in menuButtons)
            {
                if (button.PageKey == pageKey && !button.IsChild)
                    button.Visible = visible;
            }
        }

        // Call this when navigation happens elsewhere in your app.
        public void SetActivePage(string pageKey, string? title = null)
        {
            foreach (SidebarButton button in menuButtons)
            {
                button.IsSelected = button.PageKey == pageKey;

                if (button.IsSelected)
                    pageTitle.Text = title ?? button.Text;
            }

            // If the active page is a group's child, make sure that group is
            // expanded so the highlighted item is actually visible.
            foreach (var group in menuGroups)
            {
                bool childIsActive = group.Children.Exists(c => c.PageKey == pageKey);
                if (childIsActive && !group.Expanded)
                {
                    group.Expanded = true;
                    group.Parent.Expanded = true;
                    group.Parent.Invalidate();
                    group.ChildContainer.Height = group.FullHeight;
                }
            }
        }

        public void SetPageTitle(string title)
        {
            pageTitle.Text = title;
        }

        private static string GetInitials(string name)
        {
            string[] parts = name.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                return "U";

            return parts.Length == 1
                ? parts[0][0].ToString().ToUpperInvariant()
                : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
        }

        private static Color Blend(Color first, Color second, float amount)
        {
            return Color.FromArgb(
                (int)(first.R + (second.R - first.R) * amount),
                (int)(first.G + (second.G - first.G) * amount),
                (int)(first.B + (second.B - first.B) * amount));
        }

        private sealed class MenuGroup
        {
            public string GroupKey = "";
            public SidebarButton Parent = null!;
            public FlowLayoutPanel ChildContainer = null!;
            public List<SidebarButton> Children = new();
            public bool Expanded;
            public int FullHeight;
            public System.Windows.Forms.Timer AnimTimer = null!;
        }

        private sealed class InitialsAvatar : Control
        {
            [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
            public string Initials { get; set; } = "AM";

            [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
            public Color AccentColor { get; set; }

            public InitialsAvatar()
            {
                DoubleBuffered = true;
                Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                using var brush = new SolidBrush(
                    Color.FromArgb(228, 241, 252));

                e.Graphics.FillEllipse(
                    brush,
                    1,
                    1,
                    Width - 2,
                    Height - 2);

                TextRenderer.DrawText(
                    e.Graphics,
                    Initials,
                    Font,
                    ClientRectangle,
                    AccentColor,
                    TextFormatFlags.HorizontalCenter |
                    TextFormatFlags.VerticalCenter |
                    TextFormatFlags.SingleLine |
                    TextFormatFlags.NoPrefix);
            }
        }

        private sealed class SidebarButton : Button
        {
            private bool hovered;
            private bool selected;

            [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
            public string PageKey { get; set; } = "";

            [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
            public string IconGlyph { get; set; } = "";

            [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
            public Color SelectedColor { get; set; }

            [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
            public Color HoverColor { get; set; }

            /// <summary>True for a group's parent button — draws an
            /// expand/collapse chevron at the right edge instead of nothing.</summary>
            [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
            public bool IsGroupHeader { get; set; }

            [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
            public bool Expanded { get; set; }

            /// <summary>True for a group's child button — drawn smaller and
            /// indented, never gets the left accent marker when selected.</summary>
            [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
            public bool IsChild { get; set; }

            [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
            public bool IsSelected
            {
                get => selected;
                set
                {
                    selected = value;
                    Invalidate();
                }
            }

            public SidebarButton()
            {
                SetStyle(
                    ControlStyles.UserPaint |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw,
                    true);

                FlatStyle = FlatStyle.Flat;
                FlatAppearance.BorderSize = 0;

                ForeColor = Color.White;
                Font = new Font("Segoe UI", 9.5F);
                Cursor = Cursors.Hand;
                UseVisualStyleBackColor = false;
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                base.OnMouseEnter(e);
                hovered = true;
                Invalidate();
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                hovered = false;
                Invalidate();
            }

            protected override void OnGotFocus(EventArgs e)
            {
                base.OnGotFocus(e);
                Invalidate();
            }

            protected override void OnLostFocus(EventArgs e)
            {
                base.OnLostFocus(e);
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.Clear(BackColor);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                if (selected || hovered)
                {
                    var bounds = new Rectangle(
                        1, 1, Width - 2, Height - 2);

                    using var path = RoundedPath(bounds, 9);
                    using var brush = new SolidBrush(
                        selected ? SelectedColor : HoverColor);

                    e.Graphics.FillPath(brush, path);
                }

                if (selected && !IsChild)
                {
                    using var marker = new SolidBrush(
                        Color.FromArgb(55, 211, 241));

                    e.Graphics.FillRectangle(
                        marker, 1, 12, 3, Height - 24);
                }

                float scale = DeviceDpi / 96F;
                int iconLeft = IsChild ? (int)(30 * scale) : (int)(15 * scale);

                using var iconFont = new Font("Segoe MDL2 Assets", IsChild ? 12F : 16F);

                TextRenderer.DrawText(
                    e.Graphics,
                    IconGlyph,
                    iconFont,
                    new Rectangle(
                        iconLeft,
                        0,
                        (int)(28 * scale),
                        Height),
                    ForeColor,
                    TextFormatFlags.HorizontalCenter |
                    TextFormatFlags.VerticalCenter |
                    TextFormatFlags.SingleLine |
                    TextFormatFlags.NoPrefix);

                using var textFont = new Font(
                    Font,
                    selected ? FontStyle.Bold : FontStyle.Regular);

                int textLeft = IsChild ? (int)(66 * scale) : (int)(54 * scale);
                int textRight = IsGroupHeader ? (int)(30 * scale) : (int)(12 * scale);

                TextRenderer.DrawText(
                    e.Graphics,
                    Text,
                    textFont,
                    new Rectangle(
                        textLeft,
                        0,
                        Math.Max(1, Width - textLeft - textRight),
                        Height),
                    ForeColor,
                    TextFormatFlags.Left |
                    TextFormatFlags.VerticalCenter |
                    TextFormatFlags.SingleLine |
                    TextFormatFlags.EndEllipsis |
                    TextFormatFlags.NoPrefix);

                if (IsGroupHeader)
                {
                    using var chevronFont = new Font("Segoe MDL2 Assets", 9F);
                    string chevron = Expanded ? "" : ""; // ChevronUp / ChevronDown

                    TextRenderer.DrawText(
                        e.Graphics,
                        chevron,
                        chevronFont,
                        new Rectangle(Width - (int)(28 * scale), 0, (int)(20 * scale), Height),
                        ForeColor,
                        TextFormatFlags.HorizontalCenter |
                        TextFormatFlags.VerticalCenter |
                        TextFormatFlags.SingleLine |
                        TextFormatFlags.NoPrefix);
                }

                if (Focused && ShowFocusCues)
                {
                    ControlPaint.DrawFocusRectangle(
                        e.Graphics,
                        Rectangle.Inflate(ClientRectangle, -6, -6),
                        ForeColor,
                        BackColor);
                }
            }

            private static GraphicsPath RoundedPath(
                Rectangle bounds,
                int radius)
            {
                var path = new GraphicsPath();
                int diameter = radius * 2;

                path.AddArc(bounds.Left, bounds.Top,
                    diameter, diameter, 180, 90);

                path.AddArc(bounds.Right - diameter, bounds.Top,
                    diameter, diameter, 270, 90);

                path.AddArc(bounds.Right - diameter,
                    bounds.Bottom - diameter,
                    diameter, diameter, 0, 90);

                path.AddArc(bounds.Left, bounds.Bottom - diameter,
                    diameter, diameter, 90, 90);

                path.CloseFigure();

                return path;
            }
        }
    }
}
