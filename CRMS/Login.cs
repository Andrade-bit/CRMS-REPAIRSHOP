using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CRMS.winforms
{
    /// <summary>
    /// Login screen (View). Raises SignInRequested with the entered credentials
    /// and ForgotPasswordRequested when the link is clicked. The Controller/
    /// Infrastructure layer should handle actual authentication and call
    /// ShowError(...) or close this form on success.
    /// </summary>
    public class Login : Form
    {
        // ----- Public API for the Controller -----
        public event EventHandler<LoginEventArgs> SignInRequested;
        public event EventHandler ForgotPasswordRequested;

        public string Username => txtUsername.Value;
        public string Password => txtPassword.Value;

        public void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visible = !string.IsNullOrWhiteSpace(message);
        }

        private void InitializeComponent()
        {

        }

        public void SetBusy(bool busy)
        {
            btnSignIn.Enabled = !busy;
            btnSignIn.Text = busy ? "Signing in..." : "Sign In";
        }

        // ----- Brand palette (derived from the CRMS logo's blue) -----
        private static readonly Color Accent = Color.FromArgb(14, 165, 233);      // primary brand blue
        private static readonly Color AccentHover = Color.FromArgb(10, 116, 163); // deeper blue, hover/pressed
        private static readonly Color AccentDeep = Color.FromArgb(8, 99, 140);    // darkest blue, gradient end
        private static readonly Color BrandTextMuted = Color.FromArgb(196, 230, 250); // muted white-on-blue text

        private static readonly Color CardColor = Color.White;
        private static readonly Color TitleColor = Color.FromArgb(33, 37, 41);
        private static readonly Color SubtleGray = Color.FromArgb(130, 134, 145);
        private static readonly Color FieldBorder = Color.FromArgb(222, 224, 230);
        private static readonly Color FieldFill = Color.FromArgb(247, 250, 253);

        // ----- Layout constants -----
        private const int BrandWidth = 380;
        private const int FormWidth = 340;

        // ----- Controls -----
        private Panel brandPanel;
        private Panel formPanel;
        private LogoMark logoMark;
        private Label lblBrandTitle;
        private Label lblBrandTagline;

        private Label lblWelcome;
        private Label lblSubtitle;
        private Label lblUsernameCaption;
        private RoundedTextBox txtUsername;
        private Label lblPasswordCaption;
        private RoundedTextBox txtPassword;
        private LinkLabel lnkForgotPassword;
        private RoundedButton btnSignIn;
        private Label lblError;
        private Label lblFooter;

        public Login()
        {
            BuildLayout();
        }

        private void BuildLayout()
        {
            Text = "Sign In";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(920, 560);
            BackColor = CardColor;
            Font = new Font("Segoe UI", 9F);

            BuildFormPanel();
            BuildBrandPanel();

            // Fill panel must be added before the Left-docked panel so the brand
            // panel correctly claims its fixed width and the form panel fills the rest.
            Controls.Add(formPanel);
            Controls.Add(brandPanel);
        }

        private void BuildBrandPanel()
        {
            brandPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = BrandWidth,
                BackColor = Accent
            };
            brandPanel.Paint += BrandPanel_Paint;

            logoMark = new LogoMark
            {
                Size = new Size(150, 118),
                Location = new Point((BrandWidth - 150) / 2, 78),
                AccentColor = Accent,
                ScreenColor = Color.White
            };
            brandPanel.Controls.Add(logoMark);

            lblBrandTitle = new Label
            {
                Text = "Computer Repair Shop",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true
            };
            lblBrandTitle.Location = new Point((BrandWidth - lblBrandTitle.PreferredWidth) / 2, 214);
            brandPanel.Controls.Add(lblBrandTitle);

            lblBrandTagline = new Label
            {
                Text = "Repair Shop Management System",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = BrandTextMuted,
                AutoSize = true
            };
            lblBrandTagline.Location = new Point((BrandWidth - lblBrandTagline.PreferredWidth) / 2, 250);
            brandPanel.Controls.Add(lblBrandTagline);

            AddBrandBullet("Real-time ticket tracking", 372);
            AddBrandBullet("Multi-branch tenant support", 402);
            AddBrandBullet("Full staff accountability", 432);
        }

        private void AddBrandBullet(string text, int y)
        {
            var dot = new Label
            {
                Text = "●",
                Font = new Font("Segoe UI", 6F),
                ForeColor = BrandTextMuted,
                AutoSize = true,
                Location = new Point(64, y + 3)
            };
            var label = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(84, y)
            };
            brandPanel.Controls.Add(dot);
            brandPanel.Controls.Add(label);
        }

        private void BrandPanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var gradient = new LinearGradientBrush(
                brandPanel.ClientRectangle, Accent, AccentDeep, 90f))
            {
                g.FillRectangle(gradient, brandPanel.ClientRectangle);
            }

            // Soft decorative circles for depth, subtle enough to sit behind the text.
            using (var circleBrush = new SolidBrush(Color.FromArgb(14, 255, 255, 255)))
            {
                g.FillEllipse(circleBrush, -110, brandPanel.Height - 260, 320, 320);
            }
            using (var circleBrush = new SolidBrush(Color.FromArgb(10, 255, 255, 255)))
            {
                g.FillEllipse(circleBrush, brandPanel.Width - 150, -90, 220, 220);
            }
        }

        private void BuildFormPanel()
        {
            formPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardColor
            };

            int panelWidth = ClientSize.Width - BrandWidth;
            int formX = (panelWidth - FormWidth) / 2;

            lblWelcome = new Label
            {
                Text = "Welcome Back",
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                ForeColor = TitleColor,
                AutoSize = true,
                Location = new Point(formX, 96)
            };
            formPanel.Controls.Add(lblWelcome);

            lblSubtitle = new Label
            {
                Text = "Sign in to manage your repair shop",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(formX, 134)
            };
            formPanel.Controls.Add(lblSubtitle);

            // Username
            lblUsernameCaption = MakeCaption("USERNAME", formX, 188);
            formPanel.Controls.Add(lblUsernameCaption);

            txtUsername = new RoundedTextBox
            {
                Location = new Point(formX, 208),
                Size = new Size(FormWidth, 42),
                Placeholder = "Enter username",
                BackColor = FieldFill,
                FocusBorderColor = Accent
            };
            formPanel.Controls.Add(txtUsername);

            // Password
            lblPasswordCaption = MakeCaption("PASSWORD", formX, 266);
            formPanel.Controls.Add(lblPasswordCaption);

            txtPassword = new RoundedTextBox
            {
                Location = new Point(formX, 286),
                Size = new Size(FormWidth, 42),
                Placeholder = "Enter password",
                IsPassword = true,
                BackColor = FieldFill,
                FocusBorderColor = Accent
            };
            formPanel.Controls.Add(txtPassword);

            // Forgot password link
            lnkForgotPassword = new LinkLabel
            {
                Text = "Forgot password?",
                Font = new Font("Segoe UI", 8.5F),
                LinkColor = Accent,
                ActiveLinkColor = AccentHover,
                LinkBehavior = LinkBehavior.HoverUnderline,
                AutoSize = true
            };
            lnkForgotPassword.Location = new Point(formX + FormWidth - lnkForgotPassword.PreferredWidth, 336);
            lnkForgotPassword.LinkClicked += (s, e) => ForgotPasswordRequested?.Invoke(this, EventArgs.Empty);
            formPanel.Controls.Add(lnkForgotPassword);

            // Sign in button
            btnSignIn = new RoundedButton
            {
                Text = "Sign In",
                Location = new Point(formX, 372),
                Size = new Size(FormWidth, 46),
                BackColor = Accent,
                HoverColor = AccentHover,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold)
            };
            btnSignIn.Click += BtnSignIn_Click;
            formPanel.Controls.Add(btnSignIn);
            AcceptButton = btnSignIn;

            // Error label (hidden until ShowError is called)
            lblError = new Label
            {
                ForeColor = Color.FromArgb(200, 70, 70),
                Font = new Font("Segoe UI", 8F),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(FormWidth, 30),
                Location = new Point(formX, 426),
                Visible = false
            };
            formPanel.Controls.Add(lblError);

            lblFooter = new Label
            {
                Text = "© 2026 CRMS — Computer Repair Shop Management",
                Font = new Font("Segoe UI", 8F),
                ForeColor = SubtleGray,
                AutoSize = true
            };
            lblFooter.Location = new Point((panelWidth - lblFooter.PreferredWidth) / 2, ClientSize.Height - 40);
            formPanel.Controls.Add(lblFooter);
        }

        private Label MakeCaption(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(x, y)
            };
        }

        private void BtnSignIn_Click(object sender, EventArgs e)
        {
            ShowError(null);
            SignInRequested?.Invoke(this, new LoginEventArgs(Username, Password));
        }
    }

    public class LoginEventArgs : EventArgs
    {
        public string Username { get; }
        public string Password { get; }

        public LoginEventArgs(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }

    // ===================== Reusable rounded controls =====================

    /// <summary>A Panel with rounded corners, used for the card. Set BorderColor for a subtle outline.</summary>
    public class RoundedPanel : Panel
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CornerRadius { get; set; } = 12;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BorderColor { get; set; } = Color.Transparent;

        public RoundedPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = RoundedRect.Path(rect, CornerRadius))
            using (var brush = new SolidBrush(BackColor))
            {
                e.Graphics.FillPath(brush, path);
                if (BorderColor != Color.Transparent)
                {
                    using (var pen = new Pen(BorderColor))
                        e.Graphics.DrawPath(pen, path);
                }
            }
        }
    }

    /// <summary>A flat button with rounded corners and a hover color.</summary>
    public class RoundedButton : Button
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CornerRadius { get; set; } = 10;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color HoverColor { get; set; } = Color.Gray;

        private bool _hover;

        public RoundedButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            FlatAppearance.MouseOverBackColor = Color.Transparent;
            FlatAppearance.MouseDownBackColor = Color.Transparent;
            UseVisualStyleBackColor = false; // stops Windows theming from painting its own chrome under our rounded corners
            TabStop = false;
            Cursor = Cursors.Hand;
            MouseEnter += (s, e) => { _hover = true; Invalidate(); };
            MouseLeave += (s, e) => { _hover = false; Invalidate(); };
        }

        // Windows still tries to paint its own non-client border/focus-cue on flat
        // buttons in some DPI/theme combinations, showing up as a jagged sliver at
        // a corner. Eating WM_NCPAINT (0x85) stops that outside paint entirely, so
        // only our own OnPaint ever draws this control.
        protected override void WndProc(ref Message m)
        {
            const int WM_NCPAINT = 0x85;
            if (m.Msg == WM_NCPAINT)
                return;
            base.WndProc(ref m);
        }

        // OnPaint below only fills the rounded shape itself — the four small
        // corner slivers outside that shape (but still inside the control's
        // rectangular bounds) are never touched by it, so without this they
        // show through as the base Button's default (black) background.
        // Clearing with the parent's color first makes those corners blend
        // in instead.
        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            pevent.Graphics.Clear(Parent?.BackColor ?? BackColor);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = RoundedRect.Path(rect, CornerRadius))
            using (var brush = new SolidBrush(_hover ? HoverColor : BackColor))
            {
                e.Graphics.FillPath(brush, path);
            }

            // NoPrefix: without it, a bare "&" in Text (e.g. "Confirm & Generate
            // Ticket") is swallowed as a mnemonic-underline marker instead of
            // showing up — same issue Label/CheckBox have via UseMnemonic.
            TextRenderer.DrawText(e.Graphics, Text, Font, rect, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }

    /// <summary>
    /// A rounded-border text box with placeholder text support.
    /// IsPassword is applied once, up front — it is never toggled at runtime,
    /// which avoids the handle-recreation crash that can happen when
    /// UseSystemPasswordChar is flipped on an already-focused TextBox.
    /// </summary>
    public class RoundedTextBox : UserControl
    {
        private readonly TextBox _inner;
        private string _placeholder = "";
        private bool _showingPlaceholder;
        private bool _focused;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CornerRadius { get; set; } = 10;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsPassword { get; set; }

        // Left as Color.Empty (the default) this control's border never changes on
        // focus, matching every existing dialog that uses it. Only screens that
        // explicitly opt in (Login) get the highlighted focus ring.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color FocusBorderColor { get; set; } = Color.Empty;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Placeholder
        {
            get => _placeholder;
            set { _placeholder = value; ApplyPlaceholder(); }
        }

        public string Value => _showingPlaceholder ? string.Empty : _inner.Text;

        public RoundedTextBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Color.FromArgb(249, 249, 251);

            _inner = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5F),
                BackColor = BackColor
            };
            _inner.GotFocus += Inner_GotFocus;
            _inner.LostFocus += Inner_LostFocus;
            Controls.Add(_inner);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            _inner.Location = new Point(12, (Height - _inner.Height) / 2);
            _inner.Width = Width - 24;
        }

        private void ApplyPlaceholder()
        {
            if (string.IsNullOrEmpty(_inner.Text) || _showingPlaceholder)
            {
                _showingPlaceholder = true;
                _inner.Text = _placeholder;
                _inner.ForeColor = Color.FromArgb(170, 173, 182);
                // Never show dots for the placeholder itself, even on a password field.
                _inner.PasswordChar = '\0';
            }
        }

        private void Inner_GotFocus(object sender, EventArgs e)
        {
            if (_showingPlaceholder)
            {
                _showingPlaceholder = false;
                _inner.Text = "";
                _inner.ForeColor = Color.FromArgb(33, 37, 41);
                if (IsPassword)
                    _inner.PasswordChar = '●';
            }

            _focused = true;
            Invalidate();
        }

        private void Inner_LostFocus(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_inner.Text))
            {
                ApplyPlaceholder();
            }

            _focused = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            bool showFocusRing = _focused && FocusBorderColor != Color.Empty;
            var borderColor = showFocusRing ? FocusBorderColor : Color.FromArgb(222, 224, 230);
            int borderWidth = showFocusRing ? 2 : 1;

            using (var path = RoundedRect.Path(rect, CornerRadius))
            using (var fill = new SolidBrush(BackColor))
            using (var pen = new Pen(borderColor, borderWidth))
            {
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(pen, path);
            }
        }
    }

    /// <summary>Shared helper for building a rounded-rectangle GraphicsPath.</summary>
    internal static class RoundedRect
    {
        public static GraphicsPath Path(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();

            // A 0-radius arc has a zero-size bounding rectangle, which GDI+
            // rejects (ArgumentException) — fall back to a plain rectangle
            // for a genuinely square corner instead of crashing.
            if (radius <= 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            int d = radius * 2;
            var arc = new Rectangle(bounds.Location, new Size(d, d));

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - d;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - d;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    /// <summary>Shared helper for building a gear-shaped GraphicsPath (a ring of teeth around a root circle).</summary>
    internal static class GearPath
    {
        public static GraphicsPath Build(PointF center, float outerRadius, float rootRadius, int teeth)
        {
            var points = new List<PointF>(teeth * 4);
            double anglePerTooth = 360.0 / teeth;
            double toothAngle = anglePerTooth * 0.5;

            for (int i = 0; i < teeth; i++)
            {
                double baseAngle = i * anglePerTooth;
                points.Add(Polar(center, rootRadius, baseAngle));
                points.Add(Polar(center, outerRadius, baseAngle + toothAngle * 0.5));
                points.Add(Polar(center, outerRadius, baseAngle + toothAngle));
                points.Add(Polar(center, rootRadius, baseAngle + anglePerTooth * 0.5));
            }

            var path = new GraphicsPath();
            path.AddPolygon(points.ToArray());
            return path;
        }

        private static PointF Polar(PointF center, float radius, double angleDegrees)
        {
            double radians = angleDegrees * Math.PI / 180.0;
            return new PointF(
                center.X + (float)(radius * Math.Cos(radians)),
                center.Y + (float)(radius * Math.Sin(radians)));
        }
    }

    /// <summary>
    /// A code-drawn "laptop with gears" brand mark — no external image asset needed.
    /// Draws a rounded laptop screen/base in <see cref="AccentColor"/> with two
    /// gears cut out of the <see cref="ScreenColor"/> display area, matching the
    /// CRMS logo's silhouette.
    /// </summary>
    public class LogoMark : Control
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color AccentColor { get; set; } = Color.FromArgb(14, 165, 233);

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ScreenColor { get; set; } = Color.White;

        public LogoMark()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            pevent.Graphics.Clear(Parent?.BackColor ?? BackColor);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float w = Width;
            float h = Height;

            // Screen bezel
            var screenRect = new RectangleF(0, 0, w, h * 0.68f);
            using (var bezelPath = RoundedRect.Path(Rectangle.Round(screenRect), 16))
            using (var bezelBrush = new SolidBrush(AccentColor))
            {
                g.FillPath(bezelBrush, bezelPath);
            }

            // Display area
            var innerRect = new RectangleF(
                screenRect.X + 9, screenRect.Y + 9,
                screenRect.Width - 18, screenRect.Height - 18);
            using (var innerPath = RoundedRect.Path(Rectangle.Round(innerRect), 9))
            using (var innerBrush = new SolidBrush(ScreenColor))
            {
                g.FillPath(innerBrush, innerPath);
            }

            // Gears inside the display
            using (var gearBrush = new SolidBrush(AccentColor))
            {
                var bigCenter = new PointF(
                    innerRect.X + innerRect.Width * 0.40f,
                    innerRect.Y + innerRect.Height * 0.60f);
                float bigOuter = innerRect.Height * 0.36f;
                DrawGear(g, gearBrush, bigCenter, bigOuter, bigOuter * 0.72f, 8, 0.40f);

                var smallCenter = new PointF(
                    innerRect.X + innerRect.Width * 0.74f,
                    innerRect.Y + innerRect.Height * 0.30f);
                float smallOuter = innerRect.Height * 0.22f;
                DrawGear(g, gearBrush, smallCenter, smallOuter, smallOuter * 0.70f, 7, 0.38f);
            }

            // Laptop base
            var baseRect = new RectangleF(-w * 0.08f, screenRect.Bottom + 6, w * 1.16f, h * 0.13f);
            using (var basePath = RoundedRect.Path(Rectangle.Round(baseRect), 6))
            using (var baseBrush = new SolidBrush(AccentColor))
            {
                g.FillPath(baseBrush, basePath);
            }

            float notchWidth = w * 0.16f;
            var notchRect = new RectangleF(
                w / 2 - notchWidth / 2, baseRect.Y + baseRect.Height / 2 - 1.5f, notchWidth, 3f);
            using (var notchBrush = new SolidBrush(ScreenColor))
            {
                g.FillRectangle(notchBrush, notchRect);
            }
        }

        private void DrawGear(Graphics g, Brush brush, PointF center, float outerRadius, float rootRadius, int teeth, float holeRadiusRatio)
        {
            using (var gearPath = GearPath.Build(center, outerRadius, rootRadius, teeth))
            {
                g.FillPath(brush, gearPath);
            }

            float holeRadius = outerRadius * holeRadiusRatio;
            using (var holeBrush = new SolidBrush(ScreenColor))
            {
                g.FillEllipse(holeBrush, center.X - holeRadius, center.Y - holeRadius, holeRadius * 2, holeRadius * 2);
            }
        }
    }
}
