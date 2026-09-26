using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CRMS.winforms; // reuses RoundedPanel/RoundedButton from Login.cs

namespace CRMS.winforms.Shared
{
    /// <summary>
    /// Popup used from the Subscribe form to browse the three subscription
    /// tiers and pick one — picking a plan closes this dialog and hands its
    /// billing cycle + amount back to the caller to drop into the Subscribe
    /// form's own fields. Every plan/price/module here is still placeholder
    /// content (no API, no controller) — only the picking mechanism is real.
    /// </summary>
    public class SubscriptionPlanPickerDialog : Form
    {
        private static readonly Color PageBg = Color.FromArgb(247, 249, 251);
        private static readonly Color CardBorder = Color.FromArgb(227, 233, 240);
        private static readonly Color GoodBg = Color.FromArgb(233, 248, 238);
        private static readonly Color GoodText = Color.FromArgb(22, 163, 74);
        private static readonly Color FaintText = Color.FromArgb(154, 169, 184);

        private sealed record ModuleRow(string Label, bool InStarter, bool InGrowth, bool InAllAccess);

        // Matches CRMS.api's ModuleAccessController exactly — Starter only
        // gets the core three (main transactions + data collection, no
        // business intelligence); Growth gets everything All Access does
        // except Branch Management.
        private static readonly ModuleRow[] Modules =
        {
            new("Ticket workflow (Pending, Diagnose, Repair, History)", true, true, true),
            new("Customer records & search", true, true, true),
            new("Staff & Manager accounts", true, true, true),
            new("Promotions & discounts", false, true, true),
            new("Reports & analytics", false, true, true),
            new("PDF export", false, true, true),
            new("Email notifications", false, true, true),
            new("Offline-first cloud sync", false, true, true),
            new("Branch management", false, false, true),
        };

        private sealed record PlanDef(string Tier, string Name, string Description, decimal Monthly, decimal Yearly, Func<ModuleRow, bool> Includes, bool Featured);

        private static readonly PlanDef[] Plans =
        {
            new("Plan 3 · Starter", "Starter",
                "The core repair-ticket workflow for a single small shop just getting off paper logs.",
                1499m, 1249m, m => m.InStarter, false),
            new("Plan 2 · Growth", "Growth",
                "Everything All Access has, except multi-branch — for a shop running one location.",
                2999m, 2499m, m => m.InGrowth, true),
            new("Plan 1 · All Access", "All Access",
                "Every module CRMS has, including multi-branch and offline-first sync — for a shop running more than one location.",
                4999m, 4166m, m => m.InAllAccess, false),
        };

        private RoundedButton btnMonthly = null!;
        private RoundedButton btnYearly = null!;
        private bool _isYearly;
        private readonly List<(Label Price, Label Suffix, Label YearlyNote, PlanDef Plan)> _priceLabels = new();

        /// <summary>The plan the user picked, once this dialog returns DialogResult.OK.</summary>
        public string SelectedPlanName { get; private set; } = "";

        /// <summary>Same plan, as the stored key CRMS.api's ModuleAccessController
        /// expects ("AllAccess"/"Growth"/"Starter") rather than the display name.</summary>
        public string SelectedPlanKey { get; private set; } = "AllAccess";
        public string SelectedBillingCycle { get; private set; } = "Monthly";
        public decimal SelectedAmount { get; private set; }

        public SubscriptionPlanPickerDialog()
        {
            // The Monthly/Yearly toggle updates text on 9+ labels across all
            // three cards in one click — without double buffering at the form
            // level, that batch of repaints can visibly tear (old and new
            // text overlapping for a frame), which is what was showing up as
            // "glitching" text.
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();

            BuildLayout();
        }

        private void BuildLayout()
        {
            Text = "Choose a Subscription Plan";
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1120, 900);
            MinimumSize = new Size(980, 700);
            BackColor = PageBg;
            Font = new Font("Segoe UI", 9F);

            var scroller = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = PageBg, Padding = new Padding(36, 28, 36, 28) };
            Controls.Add(scroller);

            var header = new Panel { Dock = DockStyle.Top, Height = 74, BackColor = PageBg };

            var title = new Label
            {
                Text = "Three tiers, one module list",
                Font = new Font("Segoe UI", 19F, FontStyle.Bold),
                ForeColor = Color.FromArgb(20, 36, 51),
                AutoSize = true,
                Location = new Point(0, 0)
            };
            var subtitle = new Label
            {
                Text = "Pick a tier for this tenant — you can change it again later.",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(0, 32)
            };
            header.Controls.Add(title);
            header.Controls.Add(subtitle);

            var toggle = BuildBillingToggle();
            toggle.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            void RepositionToggle() => toggle.Location = new Point(header.Width - toggle.Width, (header.Height - toggle.Height) / 2 - 8);
            header.Resize += (s, e) => RepositionToggle();
            header.Controls.Add(toggle);
            RepositionToggle();

            var cardsRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 730,
                Margin = new Padding(0, 20, 0, 0),
                ColumnCount = 3,
                RowCount = 1,
                BackColor = PageBg
            };
            cardsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            cardsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            cardsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));

            for (int i = 0; i < Plans.Length; i++)
            {
                var cardHost = new Panel { Dock = DockStyle.Fill, BackColor = PageBg, Padding = new Padding(12, 6, 12, 6) };
                cardHost.Controls.Add(BuildPlanCard(Plans[i]));
                cardsRow.Controls.Add(cardHost, i, 0);
            }

            var footnote = new Label
            {
                Dock = DockStyle.Top,
                Height = 36,
                Margin = new Padding(0, 16, 0, 0),
                Text = "Design preview only — nothing here is wired to the API yet. Choosing a plan just fills in the Subscribe form's billing cycle and amount.",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = FaintText,
                AutoEllipsis = true
            };

            var content = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, AutoSize = true };
            content.Controls.Add(header);
            content.Controls.Add(cardsRow);
            content.Controls.Add(footnote);
            scroller.Controls.Add(content);

            SetPeriod(isYearly: false);
        }

        private Panel BuildBillingToggle()
        {
            var host = new RoundedPanel
            {
                Size = new Size(200, 42),
                BackColor = Color.FromArgb(240, 243, 247),
                BorderColor = CardBorder,
                CornerRadius = 21,
                Padding = new Padding(3)
            };

            btnMonthly = new RoundedButton
            {
                Text = "Monthly",
                Location = new Point(3, 3),
                Size = new Size(96, 36),
                CornerRadius = 18,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnMonthly.Click += (s, e) => SetPeriod(isYearly: false);

            btnYearly = new RoundedButton
            {
                Text = "Yearly",
                Location = new Point(101, 3),
                Size = new Size(96, 36),
                CornerRadius = 18,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnYearly.Click += (s, e) => SetPeriod(isYearly: true);

            host.Controls.Add(btnMonthly);
            host.Controls.Add(btnYearly);
            return host;
        }

        private void SetPeriod(bool isYearly)
        {
            _isYearly = isYearly;

            // Batches every control update below into one layout/paint pass
            // instead of one per control — on top of the form-level double
            // buffering, this is what actually stops the toggle from visibly
            // tearing across 9+ labels changing text at once.
            SuspendLayout();

            // Solid colors instead of Transparent for the "off" state — a
            // transparent fill on a custom-painted button can leave the
            // previous fill's pixels showing through until the next full
            // repaint, which read as ghosting/overlapping text here.
            Color inactiveFill = Color.FromArgb(240, 243, 247);

            btnMonthly.BackColor = isYearly ? inactiveFill : Theme.Accent;
            btnMonthly.ForeColor = isYearly ? Theme.TextSecondary : Color.White;
            btnMonthly.HoverColor = isYearly ? Color.FromArgb(228, 232, 238) : Theme.AccentHover;

            btnYearly.BackColor = isYearly ? Theme.Accent : inactiveFill;
            btnYearly.ForeColor = isYearly ? Color.White : Theme.TextSecondary;
            btnYearly.HoverColor = isYearly ? Theme.AccentHover : Color.FromArgb(228, 232, 238);

            foreach (var (priceLabel, suffixLabel, noteLabel, plan) in _priceLabels)
            {
                decimal amount = isYearly ? plan.Yearly : plan.Monthly;
                priceLabel.Text = amount.ToString("₱#,##0");
                suffixLabel.Text = isYearly ? "/ mo, billed yearly" : "/ mo";
                noteLabel.Text = isYearly ? "Two months free vs. paying monthly" : "";
            }

            ResumeLayout(true);
            Invalidate(true);
        }

        private Control BuildPlanCard(PlanDef plan)
        {
            var card = new RoundedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderColor = plan.Featured ? Theme.Accent : CardBorder
                , CornerRadius = 16
            };

            // RoundedPanel's Padding property only affects docked children —
            // every child here is absolutely positioned instead (that's what
            // was clipping the Platform Reports stat cards earlier), so the
            // card's inner margin is baked into padLeft/padTop directly.
            const int padLeft = 26;
            const int padTop = 24;
            const int contentWidth = 280;
            int y = padTop;

            if (plan.Featured)
            {
                var badge = new Label
                {
                    Text = "RECOMMENDED",
                    AutoSize = false,
                    Size = new Size(130, 24),
                    BackColor = Theme.Accent,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 7.75F, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Location = new Point(padLeft, y)
                };
                Theme.ApplyRoundedRegion(badge, 12);
                card.Controls.Add(badge);
                y += 34;
            }

            var tierLabel = new Label
            {
                Text = plan.Tier.ToUpperInvariant(),
                AutoSize = true,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = FaintText,
                Location = new Point(padLeft, y)
            };
            card.Controls.Add(tierLabel);
            y += 22;

            var nameLabel = new Label
            {
                Text = plan.Name,
                AutoSize = true,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(padLeft, y)
            };
            card.Controls.Add(nameLabel);
            y += 34;

            var descLabel = new Label
            {
                Text = plan.Description,
                AutoSize = false,
                Size = new Size(contentWidth, 52),
                Font = new Font("Segoe UI", 9.25F),
                ForeColor = Theme.TextSecondary,
                Location = new Point(padLeft, y)
            };
            card.Controls.Add(descLabel);
            y += 62;

            // Stacked vertically at fixed offsets, all left-aligned at the same
            // X — not positioned relative to priceLabel's width (the previous
            // "beside the price" layout broke as soon as the suffix text got
            // longer than "/ mo", since its X depended on the price's measured
            // width but its own long text could still overflow the card).
            // priceLabel's font is fixed, so its height never changes between
            // price values, making a fixed vertical offset here safe.
            var priceLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 26F, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(padLeft, y)
            };
            card.Controls.Add(priceLabel);
            y += 40;

            var suffixLabel = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(contentWidth, 0),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                Location = new Point(padLeft, y)
            };
            card.Controls.Add(suffixLabel);
            y += 20;

            var yearlyNote = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(contentWidth, 0),
                Font = new Font("Segoe UI", 8.25F, FontStyle.Bold),
                ForeColor = GoodText,
                Location = new Point(padLeft, y)
            };
            card.Controls.Add(yearlyNote);
            y += 26;

            _priceLabels.Add((priceLabel, suffixLabel, yearlyNote, plan));

            int included = 0;
            foreach (var m in Modules)
                if (plan.Includes(m)) included++;

            var divider = new Panel { Size = new Size(contentWidth, 1), Location = new Point(padLeft, y), BackColor = CardBorder };
            card.Controls.Add(divider);
            y += 12;

            var countLabel = new Label
            {
                Text = $"{included} of {Modules.Length} modules included",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                Location = new Point(padLeft, y)
            };
            card.Controls.Add(countLabel);
            y += 32;

            foreach (var m in Modules)
            {
                bool on = plan.Includes(m);

                var iconLabel = new Label
                {
                    Text = on ? "✓" : "✕",
                    AutoSize = false,
                    Size = new Size(22, 22),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    BackColor = on ? GoodBg : Color.Transparent,
                    ForeColor = on ? GoodText : Color.FromArgb(200, 206, 214),
                    Location = new Point(padLeft, y)
                };
                Theme.ApplyRoundedRegion(iconLabel, 11);

                var textLabel = new Label
                {
                    Text = m.Label,
                    AutoSize = false,
                    Size = new Size(contentWidth - 30, 32),
                    Font = new Font("Segoe UI", 9F),
                    ForeColor = on ? Theme.TextPrimary : FaintText,
                    Location = new Point(padLeft + 30, y - 1)
                };

                card.Controls.Add(iconLabel);
                card.Controls.Add(textLabel);
                y += 34;
            }

            y += 10;
            var cta = new RoundedButton
            {
                Text = $"Choose {plan.Name}",
                Location = new Point(padLeft, y),
                Size = new Size(contentWidth, 44),
                CornerRadius = 10,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                // Same style on every card, including the featured one — only
                // the "RECOMMENDED" ribbon marks Growth as different now.
                // RoundedButton has no border to draw, only a flat fill — a
                // non-featured card is pure white, so a white button on it was
                // invisible until hovered. A light tinted fill keeps it a
                // visibly distinct button instead.
                BackColor = Color.FromArgb(242, 245, 248),
                ForeColor = Theme.TextPrimary,
                HoverColor = Color.FromArgb(228, 233, 238)
            };
            cta.Click += (s, e) =>
            {
                SelectedPlanName = plan.Name;
                SelectedPlanKey = plan.Name switch
                {
                    "Starter" => "Starter",
                    "Growth" => "Growth",
                    _ => "AllAccess"
                };
                SelectedBillingCycle = _isYearly ? "Yearly" : "Monthly";
                SelectedAmount = _isYearly ? plan.Yearly : plan.Monthly;
                DialogResult = DialogResult.OK;
                Close();
            };
            card.Controls.Add(cta);

            return card;
        }
    }
}
