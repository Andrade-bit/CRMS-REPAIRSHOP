using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS.winforms; // reuses RoundedButton from Login.cs
using CRMS.winforms.Api;
using CRMS.winforms.Shared;

namespace CRMS.winforms.SuperAdmin
{
    /// <summary>Read-only popup opened by clicking a row on Superadmin's
    /// Subscriptions page — everything about that tenant's subscription in
    /// one place, including the full readable text of whichever Terms &amp;
    /// Agreement version currently applies to their plan (not just the
    /// version label the grid itself shows).</summary>
    public class SubscriptionDetailDialog : Form
    {
        public SubscriptionDetailDialog(TenantSubscriptionEntryDto tenant, string planDisplayName, TermsVersionEntryDto? terms)
        {
            BuildLayout(tenant, planDisplayName, terms);
        }

        private void BuildLayout(TenantSubscriptionEntryDto tenant, string planDisplayName, TermsVersionEntryDto? terms)
        {
            Text = tenant.CompanyName;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            // Height is set at the bottom of this method, once the real
            // bottom of the last control is known — a fixed guess here is
            // what let the Close button overlap the terms box before.
            ClientSize = new Size(480, 400);
            BackColor = Theme.PageBg;
            Font = new Font("Segoe UI", 9F);

            int contentWidth = ClientSize.Width - 48;
            int y = 20;

            Controls.Add(new Label
            {
                Text = tenant.CompanyName,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, y)
            });
            y += 28;

            Controls.Add(new Label
            {
                Text = $"{tenant.AdminName} · {tenant.AdminUsername}",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, y)
            });
            y += 36;

            // ----- KPI strip: Plan / Billing / Amount / Status -----
            int gap = 10;
            int chipWidth = (contentWidth - gap * 3) / 4;
            bool active = string.Equals(tenant.Status, "Active", StringComparison.OrdinalIgnoreCase);

            AddChip(24, y, chipWidth, "PLAN", planDisplayName, Theme.PageBg, Theme.TextPrimary);
            AddChip(24 + (chipWidth + gap), y, chipWidth, "BILLING", tenant.BillingCycle, Theme.PageBg, Theme.TextPrimary);
            AddChip(24 + (chipWidth + gap) * 2, y, chipWidth, "AMOUNT",
                tenant.Amount.HasValue ? tenant.Amount.Value.ToString("₱#,##0") : "—", Theme.PageBg, Theme.TextPrimary);
            AddChip(24 + (chipWidth + gap) * 3, y, chipWidth, "STATUS", tenant.Status,
                active ? Theme.StatusRepairedBg : Theme.StatusUnrepairableBg,
                active ? Theme.StatusRepairedText : Theme.StatusUnrepairableText);
            y += 70;

            // ----- Details -----
            var divider1 = new Panel { Location = new Point(24, y), Size = new Size(contentWidth, 1), BackColor = Theme.BorderColor };
            Controls.Add(divider1);
            y += 14;

            AddDetailRow(24, y, contentWidth, "Onboarded", tenant.CompanyCreatedAt.ToString("MMM d, yyyy"));
            y += 24;
            AddDetailRow(24, y, contentWidth, "Next billing", tenant.NextBillingDate?.ToString("MMM d, yyyy") ?? "—");
            y += 30;

            // ----- Terms & agreement -----
            var divider2 = new Panel { Location = new Point(24, y), Size = new Size(contentWidth, 1), BackColor = Theme.BorderColor };
            Controls.Add(divider2);
            y += 14;

            Controls.Add(new Label
            {
                Text = "Terms and agreement",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, y)
            });

            if (terms is not null)
            {
                var versionLabel = new Label
                {
                    Text = $"Version {terms.Version} · {planDisplayName}",
                    Font = new Font("Segoe UI", 8.5F),
                    ForeColor = Theme.TextSecondary,
                    AutoSize = true
                };
                Controls.Add(versionLabel);
                versionLabel.Location = new Point(24 + contentWidth - versionLabel.PreferredWidth, y + 2);
            }
            y += 26;

            if (terms is null)
            {
                Controls.Add(new Label
                {
                    Text = "No terms have been published for this plan yet.",
                    Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                    ForeColor = Theme.TextSecondary,
                    AutoSize = true,
                    Location = new Point(24, y)
                });
                y += 24;
            }
            else
            {
                // A scrollable Panel + wrapped Label instead of a TextBox —
                // ReadOnly alone still lets a TextBox be clicked into and its
                // text highlighted, which reads as "editable" even though
                // typing does nothing. This reads as plain static content.
                const int boxHeight = 220;
                var scrollHost = new Panel
                {
                    Location = new Point(24, y),
                    Size = new Size(contentWidth, boxHeight),
                    AutoScroll = true,
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle
                };
                var contentLabel = new Label
                {
                    Text = terms.Content,
                    Font = new Font("Segoe UI", 9.5F),
                    ForeColor = Theme.TextPrimary,
                    AutoSize = true,
                    MaximumSize = new Size(contentWidth - 20, 0),
                    Location = new Point(8, 8)
                };
                scrollHost.Controls.Add(contentLabel);
                Controls.Add(scrollHost);
                y += boxHeight;
            }

            y += 20;

            var btnClose = new RoundedButton
            {
                Text = "Close",
                Location = new Point(24, y),
                Size = new Size(contentWidth, 42),
                BackColor = Theme.Accent,
                HoverColor = Theme.AccentHover,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnClose.Click += (s, e) => Close();
            Controls.Add(btnClose);
            AcceptButton = btnClose;
            y += btnClose.Height;

            ClientSize = new Size(ClientSize.Width, y + 20);
        }

        private void AddChip(int x, int y, int width, string caption, string value, Color bg, Color fg)
        {
            var chip = new Panel { Location = new Point(x, y), Size = new Size(width, 56), BackColor = bg };
            Theme.ApplyRoundedRegion(chip, 8);
            chip.Controls.Add(new Label
            {
                Text = caption,
                Font = new Font("Segoe UI", 7F, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(10, 8)
            });
            chip.Controls.Add(new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = fg,
                AutoSize = true,
                Location = new Point(10, 27)
            });
            Controls.Add(chip);
        }

        private void AddDetailRow(int x, int y, int width, string label, string value)
        {
            Controls.Add(new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(x, y)
            });
            var valueLabel = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true
            };
            Controls.Add(valueLabel);
            valueLabel.Location = new Point(x + width - valueLabel.PreferredWidth, y);
        }
    }
}
