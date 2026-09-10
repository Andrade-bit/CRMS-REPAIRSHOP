using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CRMS.infrastructure.Controller.Staff;
using CRMS.winforms; // reuses RoundedPanel/RoundedButton/RoundedTextBox from Login.cs
namespace CRMS.winforms.StaffView
{
    /// <summary>
    /// Staff shell: sidebar (Customer / Reports) + header + tab content,
    /// switched via StaffController the same way AdminMainForm switches
    /// pages via AdminController.
    /// </summary>
    public class NewServiceRequestForm : Form
    {
        // ----- Colors (matches StaffForm's palette) -----
        private static readonly Color BgColor = Color.FromArgb(237, 239, 242);
        private static readonly Color CardColor = Color.White;
        private static readonly Color CardBorder = Color.FromArgb(232, 234, 238);
        private static readonly Color TitleColor = Color.FromArgb(33, 37, 41);
        private static readonly Color SubtleGray = Color.FromArgb(140, 144, 153);
        private static readonly Color Accent = Color.FromArgb(58, 150, 150);
        private static readonly Color AccentHover = Color.FromArgb(47, 130, 130);
        private static readonly Color AccentSoftBg = Color.FromArgb(230, 244, 244);
        private static readonly Color FieldFill = Color.FromArgb(249, 249, 251);
        // ----- Disabled-looking button (Confirm & Generate Ticket starts inactive until the terms are accepted) -----
        private static readonly Color DisabledGray = Color.FromArgb(163, 169, 181);
        private static readonly Color DisabledGrayHover = Color.FromArgb(150, 156, 168);
        // ----- Sidebar colors (now matches AdminMainForm exactly) -----
        private static readonly Color SidebarBg = Color.FromArgb(86, 182, 255);      // #56B6FF
        private static readonly Color SidebarHover = Color.FromArgb(66, 162, 235);   // #42A2EB
        private static readonly Color SidebarPressed = Color.FromArgb(46, 142, 215); // #2E8ED7
        // ----- Header -----
        private Label titleLabel;
        private Label subtitleLabel;
        // ----- Content panels (tabs) -----
        private Panel contentPanel;
        private Panel customerContent;
        private Panel reportsContent;
        private Panel _scroll;
        private ReportsCustomer reportsCustomerControl;
        private System.Windows.Forms.Timer _resizeDebounce;
        // ----- Nav -----
        private NavItem _navCustomer;
        private NavItem _navReports;
        private NavItem _navLogout;
        private NavItem _activeNavItem;
        // ----- Controller -----
        private StaffController staffController;
        public NewServiceRequestForm()
        {
            staffController = new StaffController();
            BuildShell();
        }
        // ===================== Shell (sidebar + header + content host) =====================
        private void BuildShell()
        {
            Text = "Staff Portal";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1200, 800);
            MinimumSize = new Size(900, 600);
            BackColor = BgColor;
            Font = new Font("Segoe UI", 9F);
            // --- Sidebar ---
            var sidebar = new Panel { Dock = DockStyle.Left, Width = 220, BackColor = SidebarBg };
            Controls.Add(sidebar);
            SetupSidebar(sidebar);
            // --- Header: back button + title on the left, logo + account block combined into
            // this one bar (no separate top strip) ---
            var header = new Panel { Dock = DockStyle.Top, Height = 68, BackColor = BgColor };
            Controls.Add(header);
            var logoBadge = new Panel { Size = new Size(36, 36), Location = new Point(24, 16), BackColor = SidebarBg };
            MakeRoundedRegion(logoBadge, 10);
            var logoText = new Label
            {
                Text = "CR",
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            logoBadge.Controls.Add(logoText);
            header.Controls.Add(logoBadge);
            var backBtn = new RoundedButton
            {
                Text = "←",
                Size = new Size(36, 36),
                Location = new Point(72, 16),
                BackColor = Color.White,
                HoverColor = AccentSoftBg,
                ForeColor = Accent,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                CornerRadius = 18
            };
            backBtn.Click += (s, e) => Close();
            header.Controls.Add(backBtn);
            titleLabel = new Label
            {
                Text = "New Service Request",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = TitleColor,
                AutoSize = true,
                Location = new Point(120, 14)
            };
            header.Controls.Add(titleLabel);
            subtitleLabel = new Label
            {
                Text = "Fill in all sections to generate a ticket",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(120, 38)
            };
            header.Controls.Add(subtitleLabel);
            // Right-aligned account block, combined into this same header bar.
            var avatar = new Panel { Size = new Size(36, 36), BackColor = SidebarBg };
            MakeRound(avatar);
            var avatarLabel = new Label
            {
                Text = "SN",
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            avatar.Controls.Add(avatarLabel);
            var nameLabel = new Label
            {
                Text = "Staff Name",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = TitleColor,
                AutoSize = true
            };
            var roleLabel = new Label
            {
                Text = "Staff",
                Font = new Font("Segoe UI", 8F),
                ForeColor = SubtleGray,
                AutoSize = true
            };
            header.Controls.Add(avatar);
            header.Controls.Add(nameLabel);
            header.Controls.Add(roleLabel);
            void PositionAccountBlock()
            {
                int rightEdge = header.ClientSize.Width - 24;
                avatar.Location = new Point(rightEdge - avatar.Width, (header.Height - avatar.Height) / 2);
                nameLabel.Location = new Point(avatar.Left - 10 - nameLabel.PreferredWidth, 16);
                roleLabel.Location = new Point(avatar.Left - 10 - roleLabel.PreferredWidth, 36);
            }
            PositionAccountBlock();
            header.Resize += (s, e) => PositionAccountBlock();
            // --- Content host (holds both tabs) ---
            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgColor
            };
            Controls.Add(contentPanel);
            contentPanel.BringToFront();
            SetupCustomerContent();
            SetupReportsContent();
            contentPanel.Controls.Add(reportsContent);
            contentPanel.Controls.Add(customerContent);
            // Initialize on the Customer tab
            NavigatePage("Customer", _navCustomer);
        }
        private void SetupCustomerContent()
        {
            customerContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgColor,
                Visible = false
            };
            _scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = BgColor,
                Padding = new Padding(24, 8, 24, 24)
            };
            customerContent.Controls.Add(_scroll);
            // Debounce resize so we rebuild layout only after the user stops
            // dragging/zooming, not on every intermediate pixel.
            _resizeDebounce = new System.Windows.Forms.Timer { Interval = 120 };
            _resizeDebounce.Tick += (s, e) => { _resizeDebounce.Stop(); BuildCards(); };
            _scroll.Resize += (s, e) => { _resizeDebounce.Stop(); _resizeDebounce.Start(); };
            BuildCards();
        }
        private void SetupReportsContent()
        {
            reportsContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(32, 28, 32, 28),
                Visible = false
            };
            reportsCustomerControl = new ReportsCustomer { Dock = DockStyle.Fill };
            reportsContent.Controls.Add(reportsCustomerControl);
        }
        // =========================================================
        // SIDEBAR
        // =========================================================
        private void SetupSidebar(Panel sidebarPanel)
        {
            Panel brandPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = SidebarBg
            };
            Panel logoCircle = new Panel { Size = new Size(40, 40), Location = new Point(20, 22), BackColor = Color.White };
            MakeRound(logoCircle);
            var logoIcon = new Label
            {
                Text = "🔧",
                Font = new Font("Segoe UI Emoji", 13F),
                ForeColor = SidebarBg,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            logoCircle.Controls.Add(logoIcon);
            // Brand name is two lines: "Fix Repair" (bold) over "Shop Portal" (subtitle)
            Label brandLabel = new Label
            {
                Text = "Fix Repair",
                AutoSize = true,
                Location = new Point(70, 18),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White
            };
            Label brandSubLabel = new Label
            {
                Text = "Shop Portal",
                AutoSize = true,
                Location = new Point(70, 43),
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(232, 244, 255)
            };
            brandPanel.Controls.Add(logoCircle);
            brandPanel.Controls.Add(brandLabel);
            brandPanel.Controls.Add(brandSubLabel);
            Panel navigationPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 110,
                BackColor = SidebarBg,
                Padding = new Padding(10, 10, 10, 10)
            };
            _navCustomer = new NavItem("Customer", "👤");
            _navReports = new NavItem("Reports", "📋");
            _navCustomer.Dock = DockStyle.Top;
            _navReports.Dock = DockStyle.Top;
            _navCustomer.Height = 42;
            _navReports.Height = 42;
            _navCustomer.Click += (s, e) => NavigatePage("Customer", _navCustomer);
            _navReports.Click += (s, e) => NavigatePage("Reports", _navReports);
            navigationPanel.Controls.Add(_navReports);
            navigationPanel.Controls.Add(_navCustomer);
            Panel logoutPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = SidebarBg,
                Padding = new Padding(10, 5, 10, 5)
            };
            _navLogout = new NavItem("Log out", "🚪");
            _navLogout.Dock = DockStyle.Fill;
            _navLogout.Click += (s, e) => Close();
            logoutPanel.Controls.Add(_navLogout);
            sidebarPanel.Controls.Add(logoutPanel);
            sidebarPanel.Controls.Add(navigationPanel);
            sidebarPanel.Controls.Add(brandPanel);
        }
        // =========================================================
        // NAVIGATION (mirrors AdminMainForm.NavigatePage / SwitchContent)
        // =========================================================
        private void NavigatePage(string pageName, NavItem selectedNav)
        {
            string selectedPage = staffController.NavigateTo(pageName);
            SwitchContent(selectedPage);
            SetActiveNavItem(selectedNav);
        }
        private void SwitchContent(string pageName)
        {
            customerContent.Visible = false;
            reportsContent.Visible = false;
            switch (pageName)
            {
                case "Customer":
                    customerContent.Visible = true;
                    titleLabel.Text = "New Service Request";
                    subtitleLabel.Text = "Fill in all sections to generate a ticket";
                    break;
                case "Reports":
                    reportsContent.Visible = true;
                    titleLabel.Text = "Customer Reports";
                    subtitleLabel.Text = "Track service requests across all customers";
                    break;
            }
        }
        private void SetActiveNavItem(NavItem selectedItem)
        {
            if (_activeNavItem != null && _activeNavItem != selectedItem)
            {
                _activeNavItem.SetActive(false);
            }
            selectedItem.SetActive(true);
            _activeNavItem = selectedItem;
        }
        // ===================== Cards (rebuilt on every resize) =====================
        private void BuildCards()
        {
            _scroll.SuspendLayout();
            _scroll.Controls.Clear();
            int cardWidth = Math.Max(420, _scroll.ClientSize.Width - _scroll.Padding.Horizontal);
            var customerCard = MakeCustomerCard(cardWidth);
            var deviceCard = MakeDeviceCard(cardWidth);
            var partsCard = MakePartsCard(cardWidth);
            var termsCard = MakeTermsCard(cardWidth);
            int y = 0;
            foreach (var card in new[] { customerCard, deviceCard, partsCard, termsCard })
            {
                card.Location = new Point(0, y);
                _scroll.Controls.Add(card);
                y += card.Height + 18;
            }
            _scroll.ResumeLayout();
        }
        private RoundedPanel NewCard(int width)
        {
            return new RoundedPanel
            {
                Width = width,
                BackColor = CardColor,
                CornerRadius = 16,
                BorderColor = CardBorder
            };
        }
        private void AddSectionHeader(RoundedPanel card, string icon, string title)
        {
            var iconBadge = new Panel { Size = new Size(30, 30), Location = new Point(20, 18), BackColor = AccentSoftBg };
            MakeRound(iconBadge);
            card.Controls.Add(iconBadge);
            var iconLabel = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI Emoji", 9.5F),
                Size = new Size(30, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            iconBadge.Controls.Add(iconLabel);
            var titleLbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = TitleColor,
                AutoSize = true
            };
            card.Controls.Add(titleLbl);
            titleLbl.Location = new Point(60, 18 + (30 - titleLbl.PreferredHeight) / 2);
        }
        // ----- Customer Information -----
        private RoundedPanel MakeCustomerCard(int width)
        {
            var card = NewCard(width);
            AddSectionHeader(card, "👤", "Customer Information");
            int fieldsTop = 62;
            int gap = 16;
            int triWidth = (width - 48 - gap * 2) / 3;
            int lastTriWidth = (width - 48) - triWidth * 2 - gap * 2; // absorb rounding so the row fills exactly
            int halfWidth = (width - 48 - gap) / 2;
            AddLabeledField(card, "FIRST NAME", "e.g. Maria", 24, fieldsTop, triWidth);
            AddLabeledField(card, "MIDDLE NAME", "e.g. Anne", 24 + triWidth + gap, fieldsTop, triWidth);
            AddLabeledField(card, "LAST NAME", "e.g. Santos", 24 + (triWidth + gap) * 2, fieldsTop, lastTriWidth);
            AddLabeledField(card, "PHONE NUMBER", "+1 (555) 000-0000", 24, fieldsTop + 62, halfWidth);
            AddLabeledField(card, "EMAIL ADDRESS", "customer@email.com", 24 + halfWidth + gap, fieldsTop + 62, halfWidth);
            card.Height = fieldsTop + 62 + 56 + 20;
            return card;
        }
        // ----- Device Information -----
        private RoundedPanel MakeDeviceCard(int width)
        {
            var card = NewCard(width);
            AddSectionHeader(card, "💻", "Device Information");
            int fieldsTop = 62;
            AddLabeledField(card, "DEVICE NAME / MODEL", "e.g. MacBook Pro 14\" M3 (2023)", 24, fieldsTop, width - 48);
            AddLabeledMultiline(card, "CUSTOMER'S CONCERN", "Describe the issue in the customer's own words...",
                24, fieldsTop + 62, width - 48, 72);
            card.Height = fieldsTop + 62 + 72 + 20;
            return card;
        }
        // ----- Repair Parts & Tools -----
        private RoundedPanel MakePartsCard(int width)
        {
            var card = NewCard(width);
            AddSectionHeader(card, "🔧", "Repair Parts & Tools");
            // Nothing pre-selected yet: matches the blank "New Service Request" state
            // (0 items selected / $0.00) shown in the mockup.
            var parts = new (string Name, string Price, bool Selected)[]
            {
                ("RAM Module", "$45.00", false),
                ("SSD (256 GB)", "$80.00", false),
                ("Battery Replacement", "$55.00", false),
                ("Screen / Display", "$120.00", false),
                ("Diagnostic Fee", "$25.00", false),
                ("Thermal Paste", "$15.00", false),
                ("Keyboard Replacement", "$90.00", false),
                ("Charger / Adapter", "$35.00", false),
            };
            int px = 24, py = 62;
            const int chipHeight = 52;
            int selectedCount = 0;
            decimal total = 0;
            foreach (var part in parts)
            {
                var chip = MakePartChip(part.Name, part.Price, part.Selected);
                if (px + chip.Width > width - 24)
                {
                    px = 24;
                    py += chipHeight + 10;
                }
                chip.Location = new Point(px, py);
                card.Controls.Add(chip);
                px += chip.Width + 10;
                if (part.Selected)
                {
                    selectedCount++;
                    total += decimal.Parse(part.Price.TrimStart('$'));
                }
            }
            py += chipHeight + 16;
            var totalStrip = new RoundedPanel
            {
                Location = new Point(24, py),
                Size = new Size(width - 48, 56),
                BackColor = FieldFill,
                CornerRadius = 12
            };
            card.Controls.Add(totalStrip);
            totalStrip.Controls.Add(new Label
            {
                Text = "ESTIMATED TOTAL",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(16, 10)
            });
            totalStrip.Controls.Add(new Label
            {
                Text = $"{selectedCount} items selected",
                Font = new Font("Segoe UI", 8F),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(16, 30)
            });
            var totalValue = new Label
            {
                Text = total.ToString("$0.00"),
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Accent,
                AutoSize = true
            };
            totalStrip.Controls.Add(totalValue);
            totalValue.Location = new Point(totalStrip.Width - totalValue.PreferredWidth - 16, 14);
            card.Height = py + 56 + 20;
            return card;
        }
        // ----- Terms & Agreement -----
        private RoundedPanel MakeTermsCard(int width)
        {
            var card = NewCard(width);
            AddSectionHeader(card, "📄", "Terms & Agreement");
            var termsBox = new RoundedPanel
            {
                Location = new Point(24, 62),
                Size = new Size(width - 48, 96),
                BackColor = FieldFill,
                CornerRadius = 12
            };
            card.Controls.Add(termsBox);
            termsBox.Controls.Add(new Label
            {
                Text = "By submitting this service request, the customer agrees to the following terms: " +
                       "the shop will provide a diagnostic assessment within 24-48 hours. Repair estimates " +
                       "are subject to change upon further inspection. Parts ordered are non-refundable once " +
                       "sourced. The shop is not liable for pre-existing damage or data loss during repair. " +
                       "Devices not collected within 30 days of notification may be subject to storage fees.\n\n" +
                       "24-Hour Cancellation Policy: Customers may cancel or modify a service request without " +
                       "charge if done within 24 hours of submission. After this period, a diagnostic fee may " +
                       "apply. Cancellations must be made in writing or via phone.",
                Font = new Font("Segoe UI", 7.7F),
                ForeColor = SubtleGray,
                Location = new Point(14, 10),
                Size = new Size(width - 48 - 28, 76)
            });
            var agreeCheck = new CheckBox
            {
                Text = "Customer has read and accepted the terms",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = TitleColor,
                AutoSize = true,
                Location = new Point(24, 168)
            };
            card.Controls.Add(agreeCheck);
            // Starts inactive/gray until the customer checks the box above (matches the mockup's blank state).
            var confirmBtn = new RoundedButton
            {
                Text = "Confirm & Generate Ticket",
                Location = new Point(24, 202),
                Size = new Size(width - 48, 44),
                BackColor = DisabledGray,
                HoverColor = DisabledGrayHover,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            card.Controls.Add(confirmBtn);
            card.Height = 202 + 44 + 20;
            return card;
        }
        // ----- Field helpers -----
        private void AddLabeledField(RoundedPanel card, string caption, string placeholder, int x, int y, int width)
        {
            card.Controls.Add(new Label
            {
                Text = caption,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(x, y)
            });
            card.Controls.Add(new RoundedTextBox
            {
                Location = new Point(x, y + 18),
                Size = new Size(width, 36),
                Placeholder = placeholder
            });
        }
        private void AddLabeledMultiline(RoundedPanel card, string caption, string placeholder, int x, int y, int width, int height)
        {
            card.Controls.Add(new Label
            {
                Text = caption,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(x, y)
            });
            card.Controls.Add(new RoundedMultilineBox
            {
                Location = new Point(x, y + 18),
                Size = new Size(width, height),
                Placeholder = placeholder
            });
        }
        private RoundedButton MakePartChip(string name, string price, bool selected)
        {
            var chip = new RoundedButton
            {
                Text = selected ? $"✓  {name}\n{price}" : $"{name}\n{price}",
                Height = 52,
                BackColor = selected ? AccentSoftBg : FieldFill,
                HoverColor = selected ? AccentSoftBg : Color.FromArgb(240, 241, 245),
                ForeColor = selected ? Accent : TitleColor,
                Font = new Font("Segoe UI", 8F, selected ? FontStyle.Bold : FontStyle.Regular),
                CornerRadius = 12
            };
            int textWidth = TextRenderer.MeasureText(chip.Text.Split('\n')[0], chip.Font).Width;
            int priceWidth = TextRenderer.MeasureText(price, new Font("Segoe UI", 7.5F)).Width;
            chip.Width = Math.Max(textWidth, priceWidth) + 36;
            return chip;
        }
        private static void MakeRound(Control control)
        {
            var path = new GraphicsPath();
            path.AddEllipse(0, 0, control.Width, control.Height);
            control.Region = new Region(path);
        }
        private static void MakeRoundedRegion(Control control, int radius)
        {
            if (control.Width <= 0 || control.Height <= 0) return;
            using (var path = RoundedRect.Path(new Rectangle(0, 0, control.Width, control.Height), radius))
            {
                control.Region = new Region(path);
            }
        }
        // =========================================================
        // NAVIGATION ITEM (rounded pill on hover/active, matching AdminMainForm's nav)
        // =========================================================
        private sealed class NavItem : Panel
        {
            private const int PillRadius = 10;
            private bool active;
            private readonly Label iconLabel;
            private readonly Label textLabel;
            public NavItem(string text, string icon = "")
            {
                active = false;
                BackColor = SidebarBg;
                Cursor = Cursors.Hand;
                Padding = new Padding(20, 0, 10, 0);
                iconLabel = new Label
                {
                    Text = icon,
                    Dock = DockStyle.Left,
                    Width = 30,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI Emoji", 10F),
                    Cursor = Cursors.Hand
                };
                textLabel = new Label
                {
                    Text = text,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Regular),
                    Cursor = Cursors.Hand
                };
                Controls.Add(textLabel);
                Controls.Add(iconLabel);
                textLabel.Click += (s, e) => base.OnClick(EventArgs.Empty);
                iconLabel.Click += (s, e) => base.OnClick(EventArgs.Empty);
                MouseEnter += (s, e) => { if (!active) BackColor = SidebarHover; };
                MouseLeave += (s, e) => { if (!active) BackColor = SidebarBg; };
                textLabel.MouseEnter += (s, e) => { if (!active) BackColor = SidebarHover; };
                textLabel.MouseLeave += (s, e) => { if (!active) BackColor = SidebarBg; };
                iconLabel.MouseEnter += (s, e) => { if (!active) BackColor = SidebarHover; };
                iconLabel.MouseLeave += (s, e) => { if (!active) BackColor = SidebarBg; };
                MouseDown += (s, e) => { if (!active) BackColor = SidebarPressed; };
                MouseUp += (s, e) => { if (!active) BackColor = SidebarHover; };
                // The rounded shape only becomes visible once BackColor differs from the
                // sidebar behind it (i.e. on hover/press/active) — same "pill" look as Admin's nav.
                Resize += (s, e) => MakeRoundedRegion(this, PillRadius);
            }
            public void SetActive(bool isActive)
            {
                active = isActive;
                if (active)
                {
                    BackColor = Color.White;
                    textLabel.BackColor = Color.White;
                    textLabel.ForeColor = SidebarBg;
                    textLabel.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                    iconLabel.BackColor = Color.White;
                    iconLabel.ForeColor = SidebarBg;
                }
                else
                {
                    BackColor = SidebarBg;
                    textLabel.BackColor = SidebarBg;
                    textLabel.ForeColor = Color.White;
                    textLabel.Font = new Font("Segoe UI", 10, FontStyle.Regular);
                    iconLabel.BackColor = SidebarBg;
                    iconLabel.ForeColor = Color.White;
                }
            }
        }
    }
    /// <summary>A rounded-border multiline text box with placeholder support (local to this form).</summary>
    public class RoundedMultilineBox : UserControl
    {
        private readonly TextBox _inner;
        private string _placeholder = "";
        private bool _showingPlaceholder;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CornerRadius { get; set; } = 10;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Placeholder
        {
            get => _placeholder;
            set { _placeholder = value; ApplyPlaceholder(); }
        }
        public string Value => _showingPlaceholder ? string.Empty : _inner.Text;
        public RoundedMultilineBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Color.FromArgb(249, 249, 251);
            _inner = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5F),
                BackColor = BackColor,
                Multiline = true
            };
            _inner.GotFocus += Inner_GotFocus;
            _inner.LostFocus += Inner_LostFocus;
            Controls.Add(_inner);
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            _inner.Location = new Point(12, 8);
            _inner.Size = new Size(Math.Max(0, Width - 24), Math.Max(0, Height - 16));
        }
        private void ApplyPlaceholder()
        {
            if (string.IsNullOrEmpty(_inner.Text) || _showingPlaceholder)
            {
                _showingPlaceholder = true;
                _inner.Text = _placeholder;
                _inner.ForeColor = Color.FromArgb(170, 173, 182);
            }
        }
        private void Inner_GotFocus(object sender, EventArgs e)
        {
            if (_showingPlaceholder)
            {
                _showingPlaceholder = false;
                _inner.Text = "";
                _inner.ForeColor = Color.FromArgb(33, 37, 41);
            }
        }
        private void Inner_LostFocus(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_inner.Text))
                ApplyPlaceholder();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = RoundedRect.Path(rect, CornerRadius))
            using (var fill = new SolidBrush(BackColor))
            using (var pen = new Pen(Color.FromArgb(222, 224, 230)))
            {
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(pen, path);
            }
        }
    }
}