using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CRMS.winforms.Shared
{
    /// <summary>Round avatar chip showing a person's initials, used in every role's header.</summary>
    public sealed class AvatarCircle : Panel
    {
        private readonly string initials;
        private static readonly Font InitialsFont = new Font("Segoe UI", 9.5F, FontStyle.Bold);

        // Defaults to the shared Theme accent so every existing screen (Admin,
        // Manager, Superadmin) renders exactly as before; only a caller that
        // explicitly sets this sees a different fill color.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color AccentColor { get; set; } = Theme.Accent;

        public AvatarCircle(string initials)
        {
            this.initials = initials;
            DoubleBuffered = true;
            BackColor = Theme.PageBg;
        }

        public static string GetInitials(string name)
        {
            string[] parts = name.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "?";
            if (parts.Length == 1) return parts[0].Substring(0, System.Math.Min(2, parts[0].Length)).ToUpperInvariant();
            return (parts[0][0].ToString() + parts[parts.Length - 1][0]).ToUpperInvariant();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (var brush = new SolidBrush(AccentColor))
            {
                e.Graphics.FillEllipse(brush, 0, 0, Width - 1, Height - 1);
            }

            TextRenderer.DrawText(
                e.Graphics,
                initials,
                InitialsFont,
                new Rectangle(0, 0, Width, Height),
                Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}
