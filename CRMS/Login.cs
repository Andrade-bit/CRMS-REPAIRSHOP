using System;
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

        // ----- Colors -----
        private static readonly Color BgColor = Color.FromArgb(245, 246, 248);
        private static readonly Color CardColor = Color.White;
        private static readonly Color TitleColor = Color.FromArgb(33, 37, 41);
        private static readonly Color SubtleGray = Color.FromArgb(130, 134, 145);
        private static readonly Color FieldBorder = Color.FromArgb(222, 224, 230);
        private static readonly Color FieldFill = Color.FromArgb(249, 249, 251);
        private static readonly Color Accent = Color.FromArgb(37, 99, 235);
        private static readonly Color AccentHover = Color.FromArgb(29, 78, 216);

        // ----- Controls -----
        private Label lblCompanyName;
        private Label lblSubtitle;
        private RoundedPanel cardPanel;
        private Label lblUsernameCaption;
        private RoundedTextBox txtUsername;
        private Label lblPasswordCaption;
        private RoundedTextBox txtPassword;
        private LinkLabel lnkForgotPassword;
        private RoundedButton btnSignIn;
        private Label lblError;

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
            ClientSize = new Size(400, 520);
            BackColor = BgColor;
            Font = new Font("Segoe UI", 9F);

            // --- Company name / subtitle (no logo) ---
            lblCompanyName = new Label
            {
                Text = "Computer Shop Repair",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = TitleColor,
                AutoSize = true
            };
            lblCompanyName.Location = new Point((ClientSize.Width - lblCompanyName.PreferredWidth) / 2, 48);
            Controls.Add(lblCompanyName);

            lblSubtitle = new Label
            {
                Text = "Sign in to continue",
                Font = new Font("Segoe UI", 9F),
                ForeColor = SubtleGray,
                AutoSize = true
            };
            lblSubtitle.Location = new Point((ClientSize.Width - lblSubtitle.PreferredWidth) / 2, 82);
            Controls.Add(lblSubtitle);

            // --- Card ---
            cardPanel = new RoundedPanel
            {
                Size = new Size(340, 340),
                Location = new Point((ClientSize.Width - 340) / 2, 130),
                BackColor = CardColor,
                CornerRadius = 14
            };
            Controls.Add(cardPanel);

            // Username
            lblUsernameCaption = MakeCaption("USERNAME", 24, 28);
            cardPanel.Controls.Add(lblUsernameCaption);

            txtUsername = new RoundedTextBox
            {
                Location = new Point(24, 48),
                Size = new Size(cardPanel.Width - 48, 40),
                Placeholder = "Enter username"
            };
            cardPanel.Controls.Add(txtUsername);

            // Password
            lblPasswordCaption = MakeCaption("PASSWORD", 24, 104);
            cardPanel.Controls.Add(lblPasswordCaption);

            txtPassword = new RoundedTextBox
            {
                Location = new Point(24, 124),
                Size = new Size(cardPanel.Width - 48, 40),
                Placeholder = "Enter password",
                IsPassword = true
            };
            cardPanel.Controls.Add(txtPassword);

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
            lnkForgotPassword.Location = new Point(cardPanel.Width - 24 - lnkForgotPassword.PreferredWidth, 172);
            lnkForgotPassword.LinkClicked += (s, e) => ForgotPasswordRequested?.Invoke(this, EventArgs.Empty);
            cardPanel.Controls.Add(lnkForgotPassword);

            // Sign in button
            btnSignIn = new RoundedButton
            {
                Text = "Sign In",
                Location = new Point(24, 206),
                Size = new Size(cardPanel.Width - 48, 42),
                BackColor = Accent,
                HoverColor = AccentHover,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnSignIn.Click += BtnSignIn_Click;
            cardPanel.Controls.Add(btnSignIn);
            AcceptButton = btnSignIn;

            // Error label (hidden until ShowError is called)
            lblError = new Label
            {
                ForeColor = Color.FromArgb(200, 70, 70),
                Font = new Font("Segoe UI", 8F),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(cardPanel.Width - 48, 30),
                Location = new Point(24, 258),
                Visible = false
            };
            cardPanel.Controls.Add(lblError);
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

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = RoundedRect.Path(rect, CornerRadius))
            using (var brush = new SolidBrush(_hover ? HoverColor : BackColor))
            {
                e.Graphics.FillPath(brush, path);
            }

            TextRenderer.DrawText(e.Graphics, Text, Font, rect, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
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

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CornerRadius { get; set; } = 10;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsPassword { get; set; }

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
                    _inner.PasswordChar = '\u25CF';
            }
        }

        private void Inner_LostFocus(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_inner.Text))
            {
                ApplyPlaceholder();
            }
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

    /// <summary>Shared helper for building a rounded-rectangle GraphicsPath.</summary>
    internal static class RoundedRect
    {
        public static GraphicsPath Path(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
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
}