using System.Drawing;
using System.Windows.Forms;
using CRMS.winforms; // reuses RoundedPanel from Login.cs

namespace CRMS.winforms.Shared
{
    /// <summary>A bordered KPI tile — big number + label — used on analytics/reports pages.</summary>
    public sealed class StatCard : RoundedPanel
    {
        private readonly Label valueLabel;
        private readonly Label titleLabel;

        public StatCard(string title, string value, Color accent)
        {
            BackColor = Color.White;
            CornerRadius = 14;
            BorderColor = Theme.BorderColor;
            Padding = new Padding(20, 18, 20, 18);

            var accentBar = new Panel { Size = new Size(36, 4), Location = new Point(20, 18), BackColor = accent };
            Controls.Add(accentBar);

            valueLabel = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(20, 34)
            };
            Controls.Add(valueLabel);

            titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(20, 76)
            };
            Controls.Add(titleLabel);
        }

        public void SetValue(string value) => valueLabel.Text = value;
    }
}
