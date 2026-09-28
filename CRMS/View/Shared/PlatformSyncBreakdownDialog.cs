using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CRMS.winforms; // reuses RoundedButton from Login.cs
using CRMS.winforms.Api;

namespace CRMS.winforms.Shared
{
    /// <summary>Popup behind the Superadmin header's platform sync pill —
    /// shows every tenant's own sync state, since the pill itself only ever
    /// shows the worst case across all of them.</summary>
    public class PlatformSyncBreakdownDialog : Form
    {
        private static readonly Color SyncedColor = Color.FromArgb(24, 130, 80);
        private static readonly Color SyncingColor = Theme.Accent;
        private static readonly Color OfflineColor = Color.FromArgb(163, 45, 45);
        private static readonly Color UnknownColor = Theme.TextSecondary;

        public PlatformSyncBreakdownDialog(List<TenantSyncStatusEntryDto> statuses)
        {
            BuildLayout(statuses);
        }

        private void BuildLayout(List<TenantSyncStatusEntryDto> statuses)
        {
            Text = "Sync status";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.PageBg;
            Font = new Font("Segoe UI", 9F);

            int contentWidth = 380;
            int y = 20;

            Controls.Add(new Label
            {
                Text = "Sync status by tenant",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, y)
            });
            y += 40;

            if (statuses.Count == 0)
            {
                Controls.Add(new Label
                {
                    Text = "No active tenants yet.",
                    Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                    ForeColor = Theme.TextSecondary,
                    AutoSize = true,
                    Location = new Point(24, y)
                });
                y += 30;
            }
            else
            {
                foreach (var tenant in statuses)
                {
                    var (dotColor, label) = Describe(tenant);

                    var row = new Panel { Location = new Point(24, y), Size = new Size(contentWidth, 40), BackColor = Color.White };
                    Theme.ApplyRoundedRegion(row, 8);
                    row.Controls.Add(new Label
                    {
                        Text = tenant.CompanyName,
                        Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                        ForeColor = Theme.TextPrimary,
                        AutoSize = true,
                        Location = new Point(14, 11)
                    });

                    var dot = new Label { Text = "●", Font = new Font("Segoe UI", 10F), ForeColor = dotColor, AutoSize = true };
                    var statusLabel = new Label { Text = label, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = dotColor, AutoSize = true };
                    row.Controls.Add(dot);
                    row.Controls.Add(statusLabel);
                    int statusWidth = dot.PreferredWidth + 4 + statusLabel.PreferredWidth;
                    dot.Location = new Point(contentWidth - 14 - statusWidth, 13);
                    statusLabel.Location = new Point(dot.Right + 4, 12);

                    Controls.Add(row);
                    y += 46;
                }
            }

            y += 4;
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

            ClientSize = new Size(contentWidth + 48, y + 20);
        }

        private static (Color Color, string Label) Describe(TenantSyncStatusEntryDto tenant) => tenant.State switch
        {
            "Online" when tenant.PendingCount > 0 => (SyncingColor, $"Syncing ({tenant.PendingCount})"),
            "Online" => (SyncedColor, "Synced"),
            "Offline" => (OfflineColor, tenant.PendingCount > 0 ? $"Offline ({tenant.PendingCount} pending)" : "Offline"),
            _ => (UnknownColor, "—")
        };
    }
}
