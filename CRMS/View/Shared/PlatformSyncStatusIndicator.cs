using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS.winforms.Api;

namespace CRMS.winforms.Shared
{
    /// <summary>
    /// Superadmin's header pill — the platform-wide equivalent of the other
    /// roles' SyncStatusIndicator. A Superadmin login carries no CompanyId of
    /// its own, so instead of one tenant's state this aggregates every active
    /// tenant's: worst case wins (any tenant offline beats any tenant
    /// syncing beats fully synced). Clicking it opens a breakdown per tenant.
    /// Polls every 20s, same as the per-tenant indicator.
    /// </summary>
    public sealed class PlatformSyncStatusIndicator : Panel
    {
        private static readonly Color SyncedColor = Color.FromArgb(24, 130, 80);
        private static readonly Color SyncingColor = Theme.Accent;
        private static readonly Color OfflineColor = Color.FromArgb(163, 45, 45);
        private static readonly Color UnknownColor = Theme.TextSecondary;

        private readonly Func<Task<List<TenantSyncStatusEntryDto>>> _fetchStatus;
        private readonly System.Windows.Forms.Timer _pollTimer;
        private readonly Label _dot;
        private readonly Label _text;
        private List<TenantSyncStatusEntryDto> _lastStatuses = new();

        public PlatformSyncStatusIndicator(Func<Task<List<TenantSyncStatusEntryDto>>> fetchStatus)
        {
            _fetchStatus = fetchStatus;

            AutoSize = true;
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;

            _dot = new Label
            {
                Text = "●",
                AutoSize = true,
                Font = new Font("Segoe UI", 10F),
                ForeColor = UnknownColor,
                Location = new Point(0, 1)
            };
            _text = new Label
            {
                Text = "—",
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                Location = new Point(16, 2)
            };

            Controls.Add(_dot);
            Controls.Add(_text);
            Height = 20;

            foreach (Control c in Controls)
                c.Click += (s, e) => ShowBreakdown();
            Click += (s, e) => ShowBreakdown();

            _pollTimer = new System.Windows.Forms.Timer { Interval = 20000 };
            _pollTimer.Tick += async (s, e) => await RefreshAsync();

            HandleCreated += async (s, e) =>
            {
                await RefreshAsync();
                _pollTimer.Start();
            };
            Disposed += (s, e) => _pollTimer.Dispose();
        }

        public async Task RefreshAsync()
        {
            List<TenantSyncStatusEntryDto> statuses;
            try
            {
                statuses = await _fetchStatus();
            }
            catch
            {
                return; // leave whatever was last shown — a single failed poll isn't worth alarming over
            }

            if (IsDisposed) return;
            _lastStatuses = statuses;

            if (statuses.Count == 0)
            {
                _dot.ForeColor = UnknownColor;
                _text.ForeColor = Theme.TextSecondary;
                _text.Text = "—";
            }
            else if (statuses.Any(s => s.State == "Offline"))
            {
                int offlineCount = statuses.Count(s => s.State == "Offline");
                _dot.ForeColor = OfflineColor;
                _text.ForeColor = OfflineColor;
                _text.Text = offlineCount == 1 ? "1 tenant offline" : $"{offlineCount} tenants offline";
            }
            else if (statuses.Any(s => s.State == "Online" && s.PendingCount > 0))
            {
                int totalPending = statuses.Where(s => s.State == "Online").Sum(s => s.PendingCount);
                _dot.ForeColor = SyncingColor;
                _text.ForeColor = SyncingColor;
                _text.Text = $"Syncing ({totalPending})";
            }
            else if (statuses.All(s => s.State == "Online"))
            {
                _dot.ForeColor = SyncedColor;
                _text.ForeColor = Theme.TextSecondary;
                _text.Text = "Synced";
            }
            else
            {
                _dot.ForeColor = UnknownColor;
                _text.ForeColor = Theme.TextSecondary;
                _text.Text = "—";
            }

            _text.Location = new Point(16, 2);
        }

        private void ShowBreakdown()
        {
            using var dialog = new PlatformSyncBreakdownDialog(_lastStatuses);
            dialog.ShowDialog(FindForm());
        }
    }
}
