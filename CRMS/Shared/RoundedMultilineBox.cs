using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CRMS.winforms; // reuses RoundedRect from Login.cs

namespace CRMS.winforms.Shared
{
    /// <summary>A rounded-border multiline text box with placeholder support.</summary>
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

        private void Inner_GotFocus(object? sender, EventArgs e)
        {
            if (_showingPlaceholder)
            {
                _showingPlaceholder = false;
                _inner.Text = "";
                _inner.ForeColor = Color.FromArgb(33, 37, 41);
            }
        }

        private void Inner_LostFocus(object? sender, EventArgs e)
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
