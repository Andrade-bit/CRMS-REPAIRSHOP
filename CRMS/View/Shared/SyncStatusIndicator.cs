using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS.winforms.Api;

namespace CRMS.winforms.Shared
{
    /// <summary>
    /// Small header pill showing this tenant's local-first sync state —
    /// ● Synced / ⟳ Syncing / ✕ Offline (N pending). Polls
    /// GET /api/sync/status every 20s; never blocks the UI, and degrades to a
    /// neutral "—" if the request fails rather than showing something wrong.
    /// </summary>
    public sealed class SyncStatusIndicator : Panel
    {
        private static readonly Color SyncedColor = Color.FromArgb(24, 130, 80);
        private static readonly Color SyncingColor = Theme.Accent;
        private static readonly Color OfflineColor = Color.FromArgb(163, 45, 45);
        private static readonly Color UnknownColor = Theme.TextSecondary;

        private readonly Func<Task<AuthApiClient.SyncStatusDto>> _fetchStatus;
        private readonly System.Windows.Forms.Timer _pollTimer;
        private readonly Label _dot;
        private readonly Label _text;

        public SyncStatusIndicator(Func<Task<AuthApiClient.SyncStatusDto>> fetchStatus)
        {
            _fetchStatus = fetchStatus;

            AutoSize = true;
            BackColor = Color.Transparent;

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
            AuthApiClient.SyncStatusDto status;
            try
            {
                status = await _fetchStatus();
            }
            catch
            {
                return; // leave whatever was last shown — a single failed poll isn't worth alarming over
            }

            if (IsDisposed) return;

            switch (status.State)
            {
                case "Online" when status.PendingCount > 0:
                    _dot.ForeColor = SyncingColor;
                    _text.ForeColor = SyncingColor;
                    _text.Text = $"Syncing ({status.PendingCount})";
                    break;
                case "Online":
                    _dot.ForeColor = SyncedColor;
                    _text.ForeColor = Theme.TextSecondary;
                    _text.Text = "Synced";
                    break;
                case "Offline":
                    _dot.ForeColor = OfflineColor;
                    _text.ForeColor = OfflineColor;
                    _text.Text = status.PendingCount > 0 ? $"Offline ({status.PendingCount} pending)" : "Offline";
                    break;
                default:
                    _dot.ForeColor = UnknownColor;
                    _text.ForeColor = Theme.TextSecondary;
                    _text.Text = "—";
                    break;
            }

            _text.Location = new Point(16, 2);
        }
    }
}
