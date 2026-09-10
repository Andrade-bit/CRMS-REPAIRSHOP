using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CRMS.winforms; // reuses RoundedPanel from Login.cs
using CRMS.winforms.Controllers;
using CRMS.winforms.Shared;

namespace CRMS.winforms.SuperAdmin
{
    /// <summary>
    /// Superadmin's platform-wide analytics: tenant counts, subscription
    /// breakdown, and aggregate ticket/revenue figures across every shop.
    /// </summary>
    public partial class AnalyticsReportControl : UserControl
    {
        private readonly SuperAdminController _controller;

        private StatCard cardTenants = null!;
        private StatCard cardActive = null!;
        private StatCard cardSuspended = null!;
        private StatCard cardTickets = null!;
        private StatCard cardRevenue = null!;
        private SubscriptionBreakdownBar breakdownBar = null!;

        public AnalyticsReportControl(SuperAdminController controller)
        {
            _controller = controller;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            BackColor = Theme.PageBg;
            Padding = new Padding(Theme.PageMarginX, Theme.PageMarginTop, Theme.PageMarginX, Theme.PageMarginTop);

            var statsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 118,
                BackColor = Theme.PageBg,
                WrapContents = false
            };

            cardTenants = new StatCard("Total Tenants", "0", Theme.Accent) { Size = new Size(200, 108), Margin = new Padding(0, 0, 16, 0) };
            cardActive = new StatCard("Active Subscriptions", "0", Theme.StatusRepairedText) { Size = new Size(200, 108), Margin = new Padding(0, 0, 16, 0) };
            cardSuspended = new StatCard("Suspended", "0", Color.FromArgb(200, 80, 80)) { Size = new Size(200, 108), Margin = new Padding(0, 0, 16, 0) };
            cardTickets = new StatCard("Tickets Processed", "0", Theme.StatusRepairingText) { Size = new Size(200, 108), Margin = new Padding(0, 0, 16, 0) };
            cardRevenue = new StatCard("Total Revenue", "$0.00", Theme.StatusDiagnoseText) { Size = new Size(200, 108) };

            statsPanel.Controls.Add(cardTenants);
            statsPanel.Controls.Add(cardActive);
            statsPanel.Controls.Add(cardSuspended);
            statsPanel.Controls.Add(cardTickets);
            statsPanel.Controls.Add(cardRevenue);

            var spacer2 = Theme.CreateSpacer(20);

            var breakdownCard = new RoundedPanel
            {
                Dock = DockStyle.Top,
                Height = 130,
                BackColor = Color.White,
                CornerRadius = 14,
                BorderColor = Theme.BorderColor,
                Padding = new Padding(20, 16, 20, 16)
            };
            breakdownCard.Controls.Add(new Label
            {
                Text = "Subscription Breakdown",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(20, 16)
            });
            breakdownBar = new SubscriptionBreakdownBar
            {
                Location = new Point(20, 56),
                Size = new Size(760, 28)
            };
            breakdownCard.Controls.Add(breakdownBar);
            breakdownCard.Resize += (s, e) => breakdownBar.Width = Math.Max(100, breakdownCard.ClientSize.Width - 40);

            Controls.Add(breakdownCard);
            Controls.Add(spacer2);
            Controls.Add(statsPanel);
            Controls.Add(Theme.CreatePageHeader("Analytics Reports", "Platform-wide activity across every tenant"));

            ResumeLayout(false);
        }

        /// <summary>Recomputes every figure from the controller. Call on every page navigation.</summary>
        public void LoadData()
        {
            cardTenants.SetValue(_controller.TotalTenants.ToString());
            cardActive.SetValue(_controller.ActiveTenants.ToString());
            cardSuspended.SetValue(_controller.SuspendedTenants.ToString());
            cardTickets.SetValue(_controller.TotalTicketsProcessed.ToString());
            cardRevenue.SetValue(_controller.TotalRevenue.ToString("C"));

            breakdownBar.SetCounts(_controller.ActiveTenants, _controller.SuspendedTenants);
        }

        /// <summary>Simple two-segment horizontal bar comparing active vs. suspended tenant counts.</summary>
        private sealed class SubscriptionBreakdownBar : Panel
        {
            private int _active;
            private int _suspended;

            public SubscriptionBreakdownBar()
            {
                DoubleBuffered = true;
                BackColor = Color.White;
            }

            public void SetCounts(int active, int suspended)
            {
                _active = active;
                _suspended = suspended;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                int total = _active + _suspended;
                var track = new Rectangle(0, 0, Width, Height);

                using (var trackPath = Theme.RoundedRect(track, Height / 2))
                using (var trackBrush = new SolidBrush(Theme.RowAlt))
                {
                    e.Graphics.FillPath(trackBrush, trackPath);
                }

                if (total > 0)
                {
                    int activeWidth = (int)(Width * (_active / (double)total));
                    if (activeWidth > 0)
                    {
                        var activeRect = new Rectangle(0, 0, Math.Max(Height, activeWidth), Height);
                        using var clip = new Region(Theme.RoundedRect(track, Height / 2));
                        e.Graphics.SetClip(clip, CombineMode.Replace);
                        using (var activeBrush = new SolidBrush(Theme.StatusRepairedText))
                            e.Graphics.FillRectangle(activeBrush, activeRect);
                        e.Graphics.ResetClip();
                    }
                }

                string label = total == 0
                    ? "No tenants yet"
                    : $"{_active} Active   ·   {_suspended} Suspended";

                TextRenderer.DrawText(e.Graphics, label, new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    new Rectangle(0, 0, Width, Height), Theme.TextPrimary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
    }
}
