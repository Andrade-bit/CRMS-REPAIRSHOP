using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS.winforms.Api;
using CRMS.winforms.Controllers;
using CRMS.Winforms.Admin;

namespace CRMS.winforms.ManagerView
{
    /// <summary>
    /// Manager's Dashboard page — the same rich analytics Admin sees (revenue
    /// trend, ready-for-pickup, unpaid, customer ratings, ticket pipeline,
    /// staff workload), reusing DashboardControl.DashboardCanvas exactly as
    /// Admin does. Automatically scoped to this Manager's own branch (no
    /// branch picker — Manager only ever has one), so a Manager has real
    /// branch numbers to base a promotion's Reason on. A Manager with no
    /// branch assigned sees the whole tenant, same as Admin's default.
    /// </summary>
    public partial class ManagerDashboardControl : UserControl
    {
        private static readonly Color BackgroundColor = ColorTranslator.FromHtml("#F8FAFC");

        private readonly ManagerController _controller;
        private readonly DashboardControl.DashboardCanvas _canvas;
        private readonly Panel _scrollPanel;

        /// <summary>Fires when the "View promotions" banner button is clicked —
        /// the host page navigates to Promotions.</summary>
        public event EventHandler? PromotionsRequested;

        private DateTime? _currentFrom;
        private DateTime? _currentTo;
        private string _currentRangeLabel = "This Month";
        private DashboardDto? _lastData;

        public ManagerDashboardControl(ManagerController controller)
        {
            _controller = controller;
            BackColor = BackgroundColor;

            _scrollPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = BackgroundColor };

            // Same non-Dock positioning as Admin's DashboardControl — an
            // AutoScroll panel with a Dock=Top child silently clips instead
            // of scrolling, so the canvas is explicitly positioned/sized instead.
            _canvas = new DashboardControl.DashboardCanvas { Location = Point.Empty, Height = 960, CallerCanApprovePromotions = false };
            _canvas.ReviewRequested += (s, e) => PromotionsRequested?.Invoke(this, EventArgs.Empty);
            _canvas.KpiTileClicked += ShowKpiDetail;
            _canvas.RangeChanged += OnRangeChanged;

            _scrollPanel.Controls.Add(_canvas);
            _scrollPanel.Resize += (s, e) => SyncCanvasWidth();

            Controls.Add(_scrollPanel);
        }

        private void SyncCanvasWidth()
        {
            int width = Math.Max(200, _scrollPanel.ClientSize.Width);
            if (_canvas.Width != width) _canvas.Width = width;
        }

        private async void OnRangeChanged(DateTime? from, DateTime? to, string label)
        {
            _currentFrom = from;
            _currentTo = to;
            _currentRangeLabel = label;
            await LoadData();
        }

        /// <summary>Reloads every number from the real API for the currently
        /// selected date range, scoped to this Manager's own branch. Call on
        /// every page navigation.</summary>
        public async Task LoadData()
        {
            var data = await _controller.GetBranchDashboardAsync(_currentFrom, _currentTo);
            _lastData = data;

            SyncCanvasWidth();
            _canvas.SetData(data, _currentRangeLabel);
            _scrollPanel.AutoScrollMinSize = new Size(0, _canvas.Height + 24);
        }

        /// <summary>Opens the drill-down popup for a KPI tile — same dialog
        /// Admin uses, since the data shape is identical (just branch-scoped).</summary>
        private void ShowKpiDetail(string key)
        {
            if (_lastData is null) return;

            string title, subtitle;
            string[] columns;
            string[][] rows;

            switch (key)
            {
                case "revenue":
                    title = $"Completed service value — {_currentRangeLabel}";
                    subtitle = $"{_lastData.RevenueMtdTickets.Count} resolved ticket{(_lastData.RevenueMtdTickets.Count == 1 ? "" : "s")} — repaired, cancelled, or unrepairable";
                    columns = new[] { "Ticket #", "Customer", "Device", "Resolved", "Amount" };
                    rows = _lastData.RevenueMtdTickets.Select(t => new[]
                    {
                        $"#{t.TicketId}", t.CustomerName, t.DeviceName,
                        t.Date?.ToString("MMM d, yyyy") ?? "-", FormatPeso(t.Amount)
                    }).ToArray();
                    break;

                case "ready":
                    title = "Ready for pickup";
                    subtitle = $"{_lastData.ReadyNotPickedUpTickets.Count} device{(_lastData.ReadyNotPickedUpTickets.Count == 1 ? "" : "s")} waiting to be claimed";
                    columns = new[] { "Ticket #", "Customer", "Device", "Completed", "Amount" };
                    rows = _lastData.ReadyNotPickedUpTickets.Select(t => new[]
                    {
                        $"#{t.TicketId}", t.CustomerName, t.DeviceName,
                        t.Date?.ToString("MMM d, yyyy") ?? "-", FormatPeso(t.Amount)
                    }).ToArray();
                    break;

                case "unpaid":
                    title = "Unpaid tickets";
                    subtitle = $"{_lastData.UnpaidTickets.Count} ticket{(_lastData.UnpaidTickets.Count == 1 ? "" : "s")} not yet paid";
                    columns = new[] { "Ticket #", "Customer", "Device", "Status", "Amount" };
                    rows = _lastData.UnpaidTickets.Select(t => new[]
                    {
                        $"#{t.TicketId}", t.CustomerName, t.DeviceName, t.Status, FormatPeso(t.Amount)
                    }).ToArray();
                    break;

                case "promotions":
                    title = "Promotions pending Admin's review";
                    subtitle = $"{_lastData.PendingPromotions.Count} submitted from your branch";
                    columns = new[] { "Title", "Type", "Submitted By", "Submitted" };
                    rows = _lastData.PendingPromotions.Select(p => new[]
                    {
                        p.Title, p.Type, p.StaffName, p.CreatedAt.ToString("MMM d, yyyy")
                    }).ToArray();
                    break;

                default:
                    return;
            }

            using var dialog = new KpiDetailDialog(title, subtitle, columns, rows);
            dialog.ShowDialog(FindForm());
        }

        private static string FormatPeso(decimal amount) => amount.ToString("₱#,##0.00");
    }
}
