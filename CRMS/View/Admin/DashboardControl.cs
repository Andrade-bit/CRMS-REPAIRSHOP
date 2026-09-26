using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS.winforms.Api;
using CRMS.winforms.Controllers;

namespace CRMS.Winforms.Admin
{
    /// <summary>
    /// Admin's Dashboard page — every number drawn here comes from a real
    /// aggregate over this tenant's own tickets/promotions (CRMS.api's
    /// GET /api/dashboard), not sample data. Layout/colors match the
    /// distribution-first design approved earlier: a compact KPI strip, an
    /// actionable promotions banner, then revenue trend / payment split /
    /// ticket pipeline / staff workload as the main event.
    /// </summary>
    public partial class DashboardControl : UserControl
    {
        private static readonly Color BackgroundColor = ColorTranslator.FromHtml("#F8FAFC");
        private static readonly Color SidebarColor = ColorTranslator.FromHtml("#0C6689");
        private static readonly Color AccentColor = ColorTranslator.FromHtml("#076B95");
        private static readonly Color TextColor = ColorTranslator.FromHtml("#172B42");
        private static readonly Color MutedColor = ColorTranslator.FromHtml("#6B8099");
        private static readonly Color LightTextColor = ColorTranslator.FromHtml("#8B99A9");
        private static readonly Color BorderColor = ColorTranslator.FromHtml("#DFE6ED");
        private static readonly Color BannerColor = ColorTranslator.FromHtml("#FFF4D5");
        private static readonly Color WarnColor = ColorTranslator.FromHtml("#A66D00");
        private static readonly Color DangerColor = ColorTranslator.FromHtml("#B4232D");
        private static readonly Color StarColor = ColorTranslator.FromHtml("#FAB005");

        private readonly AdminController _controller;
        private readonly DashboardCanvas _canvas;

        /// <summary>Fires when the "Review now" banner button is clicked — the host
        /// page navigates to Promotions.</summary>
        public event EventHandler? ReviewRequested;

        /// <summary>Fires when the revenue trend card is clicked — the host
        /// navigates to Reports > Monthly Sales.</summary>
        public event EventHandler? MonthlySalesRequested;

        /// <summary>Fires when the ratings card is clicked — the host navigates
        /// to Reports > Customer Feedback.</summary>
        public event EventHandler? CustomerFeedbackRequested;

        private readonly Panel _scrollPanel;

        // The currently applied date filter — both null means "This Month"
        // (the server's own default), matching the filter bar's initial state.
        // Remembered across page navigations so switching away and back to
        // Dashboard doesn't silently reset an admin's chosen range.
        private DateTime? _currentFrom;
        private DateTime? _currentTo;
        private string _currentRangeLabel = "This Month";

        // Branch scope — null means "All Branches" (every metric on the page,
        // tenant-wide, same as before this existed). Only ever shows the
        // picker at all for a tenant that actually has branches.
        private Panel _branchBar = null!;
        private ComboBox _branchCombo = null!;
        private readonly List<BranchDto> _branches = new();
        private int? _currentBranchId;

        public DashboardControl(AdminController controller)
        {
            _controller = controller;
            BackColor = BackgroundColor;

            _scrollPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = BackgroundColor };

            // Deliberately NOT Dock=Top: a docked child inside an AutoScroll
            // panel is a well-known WinForms trap — the panel silently clips
            // instead of scrolling, because docking and the auto-scroll size
            // calculation fight each other. An explicitly positioned/sized
            // child (Location + Width/Height kept in sync by hand) is the
            // combination that actually scrolls reliably.
            _canvas = new DashboardCanvas { Location = Point.Empty, Height = 960 };
            _canvas.ReviewRequested += (s, e) => ReviewRequested?.Invoke(this, EventArgs.Empty);
            _canvas.KpiTileClicked += ShowKpiDetail;
            _canvas.TrendCardClicked += (s, e) => { if (_drilldownsEnabled) MonthlySalesRequested?.Invoke(this, EventArgs.Empty); };
            _canvas.RatingsCardClicked += (s, e) => { if (_drilldownsEnabled) CustomerFeedbackRequested?.Invoke(this, EventArgs.Empty); };
            _canvas.RangeChanged += OnRangeChanged;

            _scrollPanel.Controls.Add(_canvas);
            _scrollPanel.Resize += (s, e) => SyncCanvasWidth();

            _branchBar = BuildBranchBar();

            Controls.Add(_scrollPanel);
            Controls.Add(_branchBar);

            _ = InitBranchesAsync();
        }

        /// <summary>A slim, right-aligned bar above the scrollable dashboard —
        /// hidden (zero height) until InitBranchesAsync confirms this tenant
        /// actually has branches, so most tenants never see it at all.</summary>
        private Panel BuildBranchBar()
        {
            var bar = new Panel { Dock = DockStyle.Top, Height = 0, BackColor = BackgroundColor, Visible = false };

            var label = new Label
            {
                Text = "BRANCH",
                AutoSize = true,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = MutedColor
            };

            _branchCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F),
                Width = 190,
                Height = 28
            };
            _branchCombo.SelectedIndexChanged += BranchCombo_SelectedIndexChanged;

            void Reposition()
            {
                _branchCombo.Location = new Point(bar.ClientSize.Width - _branchCombo.Width - 35, (bar.Height - _branchCombo.Height) / 2);
                label.Location = new Point(_branchCombo.Left - label.PreferredWidth - 10, (bar.Height - label.Height) / 2);
            }
            bar.Resize += (s, e) => Reposition();

            bar.Controls.Add(_branchCombo);
            bar.Controls.Add(label);
            Reposition();

            return bar;
        }

        /// <summary>Only reveals the branch picker if this tenant actually has
        /// branches — most tenants won't, so this stays hidden for them.</summary>
        private async Task InitBranchesAsync()
        {
            var branches = await _controller.GetBranchesAsync();
            if (branches is null || branches.Count == 0) return;

            _branches.Clear();
            _branches.AddRange(branches);

            _branchCombo.Items.Clear();
            _branchCombo.Items.Add("All Branches");
            foreach (var branch in _branches)
                _branchCombo.Items.Add(branch.Name);
            _branchCombo.SelectedIndex = 0;

            _branchBar.Height = 44;
            _branchBar.Visible = true;
        }

        private async void BranchCombo_SelectedIndexChanged(object? sender, EventArgs e)
        {
            int index = _branchCombo.SelectedIndex;
            _currentBranchId = index <= 0 ? null : _branches[index - 1].BranchId;
            await LoadData();
        }

        private DashboardDto? _lastData;

        private void SyncCanvasWidth()
        {
            int width = Math.Max(200, _scrollPanel.ClientSize.Width);
            if (_canvas.Width != width) _canvas.Width = width;
        }

        /// <summary>Fired by the canvas's date filter bar — either a preset chip
        /// or an applied custom range. Reloads the whole page for the new range.</summary>
        private async void OnRangeChanged(DateTime? from, DateTime? to, string label)
        {
            _currentFrom = from;
            _currentTo = to;
            _currentRangeLabel = label;
            await LoadData();
        }

        /// <summary>Reloads every number from the real API for the currently
        /// selected date range. Call on every page navigation.</summary>
        public async Task LoadData()
        {
            var data = await _controller.GetDashboardAsync(_currentFrom, _currentTo, _currentBranchId);
            _lastData = data;

            SyncCanvasWidth();
            _canvas.SetData(data, _currentRangeLabel);

            // The canvas's real content height changes with things like the
            // promotions banner — keep the scroll region matched to it exactly,
            // so the bottom row is always reachable instead of getting clipped.
            _scrollPanel.AutoScrollMinSize = new Size(0, _canvas.Height + 24);
        }

        /// <summary>Opens the drill-down popup for a KPI tile, using the already-loaded
        /// dashboard data — no extra request, since the numbers came from here too.</summary>
        // Gated off entirely when this tenant's plan doesn't include Reports &
        // Analytics ("no business intelligence") — the KPI tiles stay visible
        // (Dashboard itself is core, never gated) but stop opening a popup.
        private bool _drilldownsEnabled = true;

        /// <summary>Called once at shell startup once the tenant's module
        /// access is known.</summary>
        public void SetDrilldownsEnabled(bool enabled) => _drilldownsEnabled = enabled;

        private void ShowKpiDetail(string key)
        {
            if (!_drilldownsEnabled) return;
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
                    title = "Promotions pending";
                    subtitle = $"{_lastData.PendingPromotions.Count} awaiting your approval";
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

        // ----------------------------------------------------------
        // Drawing helpers
        // ----------------------------------------------------------

        private static float PrepareGraphics(Graphics graphics, Control control)
        {
            float scale = control.DeviceDpi / 96F;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            graphics.ScaleTransform(scale, scale);
            return scale;
        }

        private static GraphicsPath RoundedPath(RectangleF rectangle, float radius)
        {
            var path = new GraphicsPath();
            radius = Math.Min(radius, Math.Min(rectangle.Width, rectangle.Height) / 2F);

            if (radius <= 0)
            {
                path.AddRectangle(rectangle);
                return path;
            }

            float diameter = radius * 2F;
            path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static void FillRounded(Graphics graphics, RectangleF rectangle, Color color, float radius = 14F)
        {
            if (rectangle.Width <= 0 || rectangle.Height <= 0) return;
            using var path = RoundedPath(rectangle, radius);
            using var brush = new SolidBrush(color);
            graphics.FillPath(brush, path);
        }

        private static void StrokeRounded(Graphics graphics, RectangleF rectangle, Color color, float radius = 14F)
        {
            using var path = RoundedPath(rectangle, radius);
            using var pen = new Pen(color, 1F);
            graphics.DrawPath(pen, path);
        }

        private static void DrawText(
            Graphics graphics, string text, RectangleF bounds, float size, Color color,
            FontStyle style = FontStyle.Regular, StringAlignment alignment = StringAlignment.Near)
        {
            using var font = new Font("Segoe UI", size, style, GraphicsUnit.Pixel);
            using var brush = new SolidBrush(color);
            using var format = new StringFormat
            {
                Alignment = alignment,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            };
            graphics.DrawString(text, font, brush, bounds, format);
        }

        private static void DrawCard(Graphics graphics, RectangleF bounds)
        {
            FillRounded(graphics, bounds, Color.White);
            StrokeRounded(graphics, bounds, BorderColor);
        }

        private static void DrawCardHeading(Graphics graphics, RectangleF card, string title, string subtitle)
        {
            DrawText(graphics, title, new RectangleF(card.X + 25, card.Y + 19, card.Width - 50, 27), 18, TextColor, FontStyle.Bold);
            DrawText(graphics, subtitle, new RectangleF(card.X + 25, card.Y + 45, card.Width - 50, 22), 15, LightTextColor);
        }

        /// <summary>Centers an empty-state message in the card's body (below the
        /// heading) instead of pinning it near the top — otherwise a fixed-height
        /// card with no data reads as a broken, half-empty box.</summary>
        private static void DrawEmptyMessage(Graphics graphics, RectangleF card, string message)
        {
            var body = new RectangleF(card.X + 25, card.Y + 80, card.Width - 50, card.Bottom - 15 - (card.Y + 80));
            DrawText(graphics, message, body, 14, LightTextColor, FontStyle.Regular, StringAlignment.Center);
        }

        private static void DrawProgress(Graphics graphics, RectangleF bounds, float fraction, Color color)
        {
            FillRounded(graphics, bounds, ColorTranslator.FromHtml("#EDF2F5"), 6);
            fraction = Math.Clamp(fraction, 0F, 1F);
            if (fraction <= 0) return;
            FillRounded(graphics, new RectangleF(bounds.X, bounds.Y, bounds.Width * fraction, bounds.Height), color, 6);
        }

        private static string FormatPeso(decimal amount) => amount.ToString("₱#,##0.00");
        private static string FormatPesoCompact(decimal amount) => "₱" + amount.ToString("#,##0");

        private class BufferedSurface : Control
        {
            public BufferedSurface()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                TabStop = false;
            }
        }

        // ----------------------------------------------------------
        // Dashboard content
        // ----------------------------------------------------------

        private sealed class DashboardCanvas : BufferedSurface
        {
            public event EventHandler? ReviewRequested;

            /// <summary>Fires with "revenue"/"ready"/"unpaid"/"promotions" when a KPI
            /// tile is clicked, so the host can show that tile's drill-down popup.</summary>
            public event Action<string>? KpiTileClicked;

            /// <summary>Fires when the revenue trend card is clicked — the host
            /// jumps to the Reports > Monthly Sales page for the full picture.</summary>
            public event EventHandler? TrendCardClicked;

            /// <summary>Fires when the ratings card is clicked — the host jumps
            /// to the Reports > Customer Feedback page.</summary>
            public event EventHandler? RatingsCardClicked;

            /// <summary>Fires whenever the date filter bar's effective range
            /// changes — a preset chip click, or Apply on a custom range. Both
            /// From/To null means "This Month" (the server's own default).</summary>
            public event Action<DateTime?, DateTime?, string>? RangeChanged;

            private static readonly string[] KpiKeys = { "revenue", "ready", "unpaid", "promotions" };

            private readonly ReviewButton _reviewButton = new();
            private DashboardDto? _data;
            private bool _loaded;
            private string _rangeLabel = "This Month";
            private readonly RectangleF[] _kpiBounds = new RectangleF[4];
            private int _hoveredKpiIndex = -1;

            // ----- Date filter bar -----
            private const float FilterBarY = 54, FilterBarHeight = 32;
            private const float CustomRowGap = 12, CustomRowHeight = 30;
            private const float KpiGapAfterFilter = 20;

            private string _activeRangeMode = "month";
            private bool _customExpanded;
            private readonly PillButton[] _rangeChips;
            private readonly Label _fromCaption = new() { Text = "From", AutoSize = true, ForeColor = MutedColor, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            private readonly Label _toCaption = new() { Text = "To", AutoSize = true, ForeColor = MutedColor, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            private readonly DateTimePicker _fromPicker = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "MMM d, yyyy", Font = new Font("Segoe UI", 9F), Value = DateTime.Today.AddDays(-29) };
            private readonly DateTimePicker _toPicker = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "MMM d, yyyy", Font = new Font("Segoe UI", 9F), Value = DateTime.Today };
            private readonly PillButton _applyButton;
            private readonly Label _showingLabel = new() { AutoSize = true, ForeColor = AccentColor, Font = new Font("Segoe UI", 9F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleRight };

            // Revenue trend hover: which day (if any) the mouse is nearest to,
            // for the crosshair + tooltip — recomputed every OnPaint since the
            // plot rectangle/points shift with the window size.
            private RectangleF _heroBounds;
            private RectangleF _ratingsBounds;
            private RectangleF _trendPlotBounds;
            private PointF[] _trendPoints = Array.Empty<PointF>();
            private List<RevenuePointDto> _trendData = new();
            private int _hoveredTrendIndex = -1;
            private bool _hoveredHero;
            private bool _hoveredRatings;

            public DashboardCanvas()
            {
                BackColor = BackgroundColor;
                _reviewButton.Text = "Review now";
                _reviewButton.Click += (s, e) => ReviewRequested?.Invoke(this, EventArgs.Empty);
                Controls.Add(_reviewButton);

                var chipDefs = new[] { "today", "7d", "30d", "month", "year", "custom" };
                _rangeChips = new PillButton[chipDefs.Length];
                for (int i = 0; i < chipDefs.Length; i++)
                {
                    string mode = chipDefs[i];
                    var chip = new PillButton { Text = ChipLabel(mode), Tag = mode, IsActive = mode == _activeRangeMode };
                    chip.Click += (s, e) => SetActiveMode(mode);
                    _rangeChips[i] = chip;
                    Controls.Add(chip);
                }

                _applyButton = new PillButton { Text = "Apply", IsActive = true };
                _applyButton.Click += ApplyButton_Click;

                _fromPicker.ValueChanged += (s, e) => { if (_fromPicker.Value > _toPicker.Value) _toPicker.Value = _fromPicker.Value; };
                _toPicker.ValueChanged += (s, e) => { if (_toPicker.Value < _fromPicker.Value) _fromPicker.Value = _toPicker.Value; };

                Controls.Add(_fromCaption);
                Controls.Add(_fromPicker);
                Controls.Add(_toCaption);
                Controls.Add(_toPicker);
                Controls.Add(_applyButton);
                Controls.Add(_showingLabel);

                UpdateShowingLabel();
                ArrangeFilterBar();
            }

            private static string ChipLabel(string mode) => mode switch
            {
                "today" => "Today",
                "7d" => "7 Days",
                "30d" => "30 Days",
                "month" => "This Month",
                "year" => "This Year",
                "custom" => "Custom",
                _ => mode
            };

            private void UpdateShowingLabel() => _showingLabel.Text = "Showing: " + _rangeLabel;

            /// <summary>Client-side mirror of the API's own default-range math —
            /// used only to label the filter bar and to build the from/to values
            /// sent to the server for every preset except "This Month" (which
            /// deliberately sends null/null so it always matches the server's
            /// own default exactly, with no risk of client/server date drift).</summary>
            private static (DateTime? From, DateTime? To, string Label) ComputeRange(
                string mode, DateTime? customFrom = null, DateTime? customTo = null)
            {
                var today = DateTime.Today;
                switch (mode)
                {
                    case "today":
                        return (today, today, today.ToString("MMM d, yyyy"));
                    case "7d":
                    {
                        var f = today.AddDays(-6);
                        return (f, today, $"{f:MMM d} – {today:MMM d, yyyy}");
                    }
                    case "30d":
                    {
                        var f = today.AddDays(-29);
                        return (f, today, $"{f:MMM d} – {today:MMM d, yyyy}");
                    }
                    case "year":
                    {
                        var f = new DateTime(today.Year, 1, 1);
                        return (f, today, $"{f:MMM d} – {today:MMM d, yyyy}");
                    }
                    case "custom":
                    {
                        var f = (customFrom ?? today).Date;
                        var t = (customTo ?? today).Date;
                        return (f, t, $"{f:MMM d, yyyy} – {t:MMM d, yyyy}");
                    }
                    case "month":
                    default:
                    {
                        var f = new DateTime(today.Year, today.Month, 1);
                        return (null, null, $"{f:MMM d} – {today:MMM d, yyyy}");
                    }
                }
            }

            private void SetActiveMode(string mode)
            {
                _activeRangeMode = mode;
                foreach (var chip in _rangeChips)
                    chip.IsActive = (string)chip.Tag! == mode;

                bool wasExpanded = _customExpanded;
                _customExpanded = mode == "custom";

                DateTime? from = null, to = null;
                if (mode == "custom")
                {
                    _rangeLabel = "Pick a range and click Apply";
                }
                else
                {
                    (from, to, _rangeLabel) = ComputeRange(mode);
                }
                UpdateShowingLabel();

                if (_customExpanded != wasExpanded)
                    RelayoutForRangeChange();
                else
                    ArrangeFilterBar();

                Invalidate();

                if (mode != "custom")
                    RangeChanged?.Invoke(from, to, _rangeLabel);
            }

            private void ApplyButton_Click(object? sender, EventArgs e)
            {
                var (from, to, label) = ComputeRange("custom", _fromPicker.Value, _toPicker.Value);
                _rangeLabel = label;
                UpdateShowingLabel();
                ArrangeFilterBar();
                RangeChanged?.Invoke(from, to, label);
            }

            /// <summary>Everything below the filter bar shifts when the custom
            /// range row shows/hides — recompute the canvas's own height (same
            /// math SetData uses) so the scroll region and every row beneath it
            /// stay in sync instead of overlapping or leaving a gap.</summary>
            private void RelayoutForRangeChange()
            {
                float scale = DeviceDpi / 96F;
                Height = (int)Math.Ceiling(ComputeContentHeight(_data) * scale);
                ArrangeFilterBar();
                ArrangeReviewButton();
                Invalidate();
            }

            /// <summary>Positions every filter-bar control in physical pixels,
            /// mirroring ArrangeReviewButton's own logical-to-physical DPI
            /// conversion so this row lands exactly where OnPaint's KPI row
            /// (computed from the same ComputeKpiY offset) expects it.</summary>
            private void ArrangeFilterBar()
            {
                float scale = DeviceDpi / 96F;
                const float left = 35F;
                const float chipGap = 8F;
                float[] chipWidths = { 60F, 78F, 84F, 96F, 84F, 68F };

                float x = left;
                for (int i = 0; i < _rangeChips.Length; i++)
                {
                    float w = chipWidths[i];
                    _rangeChips[i].SetBounds(
                        (int)Math.Round(x * scale), (int)Math.Round(FilterBarY * scale),
                        (int)Math.Round(w * scale), (int)Math.Round(FilterBarHeight * scale));
                    x += w + chipGap;
                }

                // "Showing: ..." — right-aligned; its own real (already-physical)
                // Width is used directly since the text length varies with the
                // selected range (a custom range reads much longer than "Today").
                float contentWidth = ClientSize.Width / scale - 70;
                int physicalRight = (int)Math.Round((left + contentWidth) * scale);
                int showingY = (int)Math.Round(FilterBarY * scale) + (int)Math.Round(FilterBarHeight * scale - _showingLabel.Height) / 2;
                _showingLabel.Location = new Point(physicalRight - _showingLabel.Width, showingY);

                bool showCustom = _customExpanded;
                _fromCaption.Visible = showCustom;
                _fromPicker.Visible = showCustom;
                _toCaption.Visible = showCustom;
                _toPicker.Visible = showCustom;
                _applyButton.Visible = showCustom;
                if (!showCustom) return;

                const float rowY = FilterBarY + FilterBarHeight + CustomRowGap;
                const float captionFromW = 32F, captionToW = 18F, pickerW = 118F, applyW = 68F, smallGap = 6F, fieldGap = 14F;
                float cx = left;

                _fromCaption.Location = new Point((int)Math.Round(cx * scale), (int)Math.Round(rowY * scale) + (int)Math.Round((CustomRowHeight * scale - _fromCaption.Height) / 2F));
                cx += captionFromW + smallGap;

                _fromPicker.SetBounds((int)Math.Round(cx * scale), (int)Math.Round(rowY * scale), (int)Math.Round(pickerW * scale), (int)Math.Round(CustomRowHeight * scale));
                cx += pickerW + fieldGap;

                _toCaption.Location = new Point((int)Math.Round(cx * scale), (int)Math.Round(rowY * scale) + (int)Math.Round((CustomRowHeight * scale - _toCaption.Height) / 2F));
                cx += captionToW + smallGap;

                _toPicker.SetBounds((int)Math.Round(cx * scale), (int)Math.Round(rowY * scale), (int)Math.Round(pickerW * scale), (int)Math.Round(CustomRowHeight * scale));
                cx += pickerW + fieldGap;

                _applyButton.SetBounds((int)Math.Round(cx * scale), (int)Math.Round(rowY * scale), (int)Math.Round(applyW * scale), (int)Math.Round(CustomRowHeight * scale));
            }

            /// <summary>The Y offset the KPI row (and everything below it) starts
            /// at — shared by OnPaint, ComputeContentHeight, and
            /// ArrangeReviewButton so they can never drift out of sync with each
            /// other as the filter bar's expanded/collapsed state changes.</summary>
            private float ComputeKpiY() =>
                FilterBarY + FilterBarHeight + (_customExpanded ? CustomRowGap + CustomRowHeight : 0) + KpiGapAfterFilter;

            private int HitTestKpi(Point clientLocation)
            {
                float scale = DeviceDpi / 96F;
                float mx = clientLocation.X / scale;
                float my = clientLocation.Y / scale;

                for (int i = 0; i < _kpiBounds.Length; i++)
                    if (_kpiBounds[i].Contains(mx, my)) return i;
                return -1;
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                float scale = DeviceDpi / 96F;
                float mx = e.Location.X / scale;
                float my = e.Location.Y / scale;

                int index = HitTestKpi(e.Location);
                bool overHero = _heroBounds.Contains(mx, my);
                bool overRatings = _ratingsBounds.Contains(mx, my);

                int trendIndex = -1;
                if (overHero && _trendPoints.Length > 0 && _trendPlotBounds.Width > 0 &&
                    mx >= _trendPlotBounds.Left - 10 && mx <= _trendPlotBounds.Right + 10)
                {
                    float fraction = Math.Clamp((mx - _trendPlotBounds.Left) / _trendPlotBounds.Width, 0F, 1F);
                    trendIndex = (int)Math.Round(fraction * (_trendPoints.Length - 1));
                }

                Cursor = (index >= 0 || overHero || overRatings) ? Cursors.Hand : Cursors.Default;

                if (index != _hoveredKpiIndex || overHero != _hoveredHero || overRatings != _hoveredRatings || trendIndex != _hoveredTrendIndex)
                {
                    _hoveredKpiIndex = index;
                    _hoveredHero = overHero;
                    _hoveredRatings = overRatings;
                    _hoveredTrendIndex = trendIndex;
                    Invalidate();
                }
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                if (_hoveredKpiIndex != -1 || _hoveredHero || _hoveredRatings || _hoveredTrendIndex != -1)
                {
                    _hoveredKpiIndex = -1;
                    _hoveredHero = false;
                    _hoveredRatings = false;
                    _hoveredTrendIndex = -1;
                    Invalidate();
                }
            }

            protected override void OnMouseClick(MouseEventArgs e)
            {
                base.OnMouseClick(e);
                int index = HitTestKpi(e.Location);
                if (index >= 0)
                {
                    KpiTileClicked?.Invoke(KpiKeys[index]);
                    return;
                }

                if (_hoveredHero) TrendCardClicked?.Invoke(this, EventArgs.Empty);
                else if (_hoveredRatings) RatingsCardClicked?.Invoke(this, EventArgs.Empty);
            }

            public void SetData(DashboardDto? data, string rangeLabel = "This Month")
            {
                _data = data;
                _loaded = true;
                _rangeLabel = rangeLabel;
                UpdateShowingLabel();

                // OnPaint below draws through graphics.ScaleTransform(scale, scale)
                // (scale = DeviceDpi / 96), so on any monitor above 100% scaling the
                // drawn content is physically taller than its own logical numbers —
                // the control's real Height has to be scaled up to match, or the
                // bottom of the drawing gets clipped by the control's own bounds
                // no matter how far the outer panel scrolls.
                float scale = DeviceDpi / 96F;
                Height = (int)Math.Ceiling(ComputeContentHeight(data) * scale);

                ArrangeReviewButton();
                ArrangeFilterBar();
                Invalidate();
            }

            /// <summary>Mirrors the vertical offsets OnPaint actually lays rows
            /// out at (in the same logical/96dpi units OnPaint's own layout math
            /// uses), so the canvas's own Height (and, through it, the scroll
            /// panel's scrollable area) always matches what's really drawn —
            /// including the promotions banner (only sometimes shown) and the
            /// custom date range row (only shown while "Custom" is active).</summary>
            private int ComputeContentHeight(DashboardDto? data)
            {
                const int kpiHeight = 118, gap = 18, heroRowHeight = 320, thirdRowHeight = 260, bottomPad = 40;
                float y = ComputeKpiY() + kpiHeight + gap;
                if ((data?.PromotionsPendingCount ?? 0) > 0) y += 76;
                y += heroRowHeight + gap;
                y += thirdRowHeight + bottomPad;
                return (int)Math.Ceiling(y);
            }

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                ArrangeReviewButton();
                ArrangeFilterBar();
            }

            private void ArrangeReviewButton()
            {
                int pending = _data?.PromotionsPendingCount ?? 0;
                _reviewButton.Visible = pending > 0;
                if (pending <= 0) return;

                // Sits inside the approval banner, right after the KPI tile row
                // (matching OnPaint's own layout math), vertically centered in
                // the banner's 58px height. ClientSize/SetBounds are real
                // physical pixels, but OnPaint draws everything through a DPI
                // ScaleTransform, so the same scale has to be applied here for
                // the button to land on top of the drawn banner instead of
                // drifting off it at >100% scaling.
                float scale = DeviceDpi / 96F;
                float bannerY = ComputeKpiY() + 118 + 18;
                float buttonY = bannerY + (58 - 32) / 2F;
                _reviewButton.SetBounds((int)(ClientSize.Width - 173 * scale), (int)(buttonY * scale), (int)(118 * scale), (int)(32 * scale));
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                Graphics graphics = e.Graphics;
                graphics.Clear(BackgroundColor);

                float scale = PrepareGraphics(graphics, this);
                float width = ClientSize.Width / scale;

                float left = 35;
                float contentWidth = width - 70;
                float gap = 18;

                // The shell header already shows "Dashboard" as the page
                // name — this subtitle is the only text drawn up here now.
                bool isDefaultRange = _activeRangeMode == "month";
                string subtitleText = isDefaultRange
                    ? "Welcome back — here's what's happening at your shop today"
                    : $"Showing data for {_rangeLabel}";
                DrawText(graphics, subtitleText, new RectangleF(left, 15, contentWidth, 27), 17, isDefaultRange ? MutedColor : AccentColor);

                if (!_loaded)
                {
                    DrawText(graphics, "Loading dashboard...", new RectangleF(left, ComputeKpiY(), contentWidth, 30), 15, MutedColor);
                    return;
                }

                if (_data is null)
                {
                    DrawText(graphics, "Could not load dashboard data. Try switching pages and back.",
                        new RectangleF(left, ComputeKpiY(), contentWidth, 30), 15, DangerColor);
                    return;
                }

                // ----- Bento row 1: four clickable KPI tiles -----
                float kpiY = ComputeKpiY();
                const float kpiHeight = 118;
                float kpiWidth = (contentWidth - gap * 3) / 4F;
                for (int i = 0; i < 4; i++)
                    _kpiBounds[i] = new RectangleF(left + i * (kpiWidth + gap), kpiY, kpiWidth, kpiHeight);

                DrawKpiTiles(graphics, _kpiBounds, _data, _hoveredKpiIndex);

                float nextY = kpiY + kpiHeight + gap;
                if (_data.PromotionsPendingCount > 0)
                {
                    DrawApprovalBanner(graphics, new RectangleF(left, nextY, contentWidth, 58), _data.PromotionsPendingCount);
                    nextY += 76;
                }

                // ----- Bento row 2: hero revenue trend + customer ratings -----
                float heroWidth = (contentWidth - gap) * 0.62F;
                float ratingsWidth = contentWidth - gap - heroWidth;
                float ratingsX = left + heroWidth + gap;
                const float heroRowHeight = 320;

                _heroBounds = new RectangleF(left, nextY, heroWidth, heroRowHeight);
                _ratingsBounds = new RectangleF(ratingsX, nextY, ratingsWidth, heroRowHeight);
                DrawRevenueCard(graphics, _heroBounds, _data.RevenueTrend, _hoveredHero, _hoveredTrendIndex);
                DrawRatingsCard(graphics, _ratingsBounds, _data, _hoveredRatings);

                // ----- Bento row 3: payment / pipeline / staff, three across -----
                float thirdRowY = nextY + heroRowHeight + gap;
                float thirdWidth = (contentWidth - gap * 2) / 3F;
                const float thirdRowHeight = 260;

                DrawPaymentCard(graphics, new RectangleF(left, thirdRowY, thirdWidth, thirdRowHeight), _data.PaymentBreakdown, _rangeLabel);
                DrawPipelineCard(graphics, new RectangleF(left + thirdWidth + gap, thirdRowY, thirdWidth, thirdRowHeight), _data.TicketPipeline);
                DrawStaffCard(graphics, new RectangleF(left + (thirdWidth + gap) * 2, thirdRowY, thirdWidth, thirdRowHeight), _data.StaffWorkload);
            }

            private static readonly (string Title, Color IconBg, Color IconFg, string Glyph)[] KpiStyles =
            {
                ("REVENUE", ColorTranslator.FromHtml("#DCF4E7"), ColorTranslator.FromHtml("#188250"), "₱"),
                ("READY FOR PICKUP", ColorTranslator.FromHtml("#FFF4D6"), ColorTranslator.FromHtml("#966C0C"), "⏱"),
                ("UNPAID TICKETS", ColorTranslator.FromHtml("#FBDEDE"), ColorTranslator.FromHtml("#A32D2D"), "₱"),
                ("PENDING PROMOTIONS", ColorTranslator.FromHtml("#E9E0FA"), ColorTranslator.FromHtml("#6642AF"), "🏷"),
            };

            /// <summary>Four individual bento tiles (not one divided strip) — each
            /// its own rounded card with a colored icon chip, a big value, and a
            /// one-line hint of what clicking it opens.</summary>
            private static void DrawKpiTiles(Graphics graphics, RectangleF[] tiles, DashboardDto data, int hoveredIndex)
            {
                string[] values =
                {
                    FormatPesoCompact(data.RevenueMtd),
                    data.ReadyNotPickedUp.ToString(),
                    data.UnpaidTicketsCount.ToString(),
                    data.PromotionsPendingCount.ToString()
                };
                string[] hints =
                {
                    "Click for breakdown",
                    data.ReadyNotPickedUp > 0 ? "Waiting on customers" : "All clear",
                    data.UnpaidTicketsCount > 0 ? "Needs follow-up" : "All settled",
                    data.PromotionsPendingCount > 0 ? "Awaiting your review" : "Nothing pending"
                };
                Color[] hintColors =
                {
                    LightTextColor,
                    data.ReadyNotPickedUp > 0 ? WarnColor : LightTextColor,
                    data.UnpaidTicketsCount > 0 ? DangerColor : LightTextColor,
                    LightTextColor
                };

                for (int i = 0; i < tiles.Length; i++)
                {
                    var bounds = tiles[i];
                    var (title, iconBg, iconFg, glyph) = KpiStyles[i];

                    DrawCard(graphics, bounds);
                    if (i == hoveredIndex)
                    {
                        // A lifted shadow-card look on hover, without an actual
                        // shadow API — a slightly larger, tinted rounded rect
                        // behind the normal card reads the same way at this size.
                        using var hoverBrush = new SolidBrush(ColorTranslator.FromHtml("#F1F6F9"));
                        using var hoverPath = RoundedPath(bounds, 14);
                        graphics.FillPath(hoverBrush, hoverPath);
                        StrokeRounded(graphics, bounds, ColorTranslator.FromHtml("#D5E3ED"), 14);
                    }

                    const float pad = 18;
                    var iconRect = new RectangleF(bounds.Right - pad - 30, bounds.Y + pad, 30, 30);
                    using (var iconBrush = new SolidBrush(iconBg))
                        graphics.FillEllipse(iconBrush, iconRect);
                    DrawText(graphics, glyph, iconRect, 14, iconFg, FontStyle.Bold, StringAlignment.Center);

                    DrawText(graphics, title, new RectangleF(bounds.X + pad, bounds.Y + pad, bounds.Width - pad * 2 - 34, 18), 10.5F, LightTextColor, FontStyle.Bold);
                    DrawText(graphics, values[i], new RectangleF(bounds.X + pad, bounds.Y + pad + 22, bounds.Width - pad * 2, 34), 25, TextColor, FontStyle.Bold);
                    DrawText(graphics, hints[i], new RectangleF(bounds.X + pad, bounds.Bottom - pad - 20, bounds.Width - pad * 2, 20), 11, hintColors[i], FontStyle.Bold);
                }
            }

            /// <summary>New tile: average rating, a star breakdown, and the most
            /// recent comment — the one piece of "how are we actually doing"
            /// feedback that nothing else on the dashboard surfaces.</summary>
            private static void DrawRatingsCard(Graphics graphics, RectangleF card, DashboardDto data, bool hovered)
            {
                DrawCard(graphics, card);
                if (hovered)
                    StrokeRounded(graphics, RectangleF.Inflate(card, -0.5F, -0.5F), AccentColor, 14);
                DrawText(graphics, "Customer Ratings", new RectangleF(card.X + 25, card.Y + 19, card.Width - 50, 27), 18, TextColor, FontStyle.Bold);
                if (hovered)
                    DrawText(graphics, "View all →", new RectangleF(card.Right - 115, card.Y + 22, 90, 20), 11, AccentColor, FontStyle.Bold, StringAlignment.Far);

                if (data.RatingCount == 0)
                {
                    DrawText(graphics, "No reviews yet", new RectangleF(card.X + 25, card.Y + 45, card.Width - 50, 22), 15, LightTextColor);
                    DrawEmptyMessage(graphics, card, "Feedback recorded at pickup will show up here.");
                    return;
                }

                DrawText(graphics, $"from {data.RatingCount} review{(data.RatingCount == 1 ? "" : "s")}", new RectangleF(card.X + 25, card.Y + 45, card.Width - 50, 22), 15, LightTextColor);

                float bigNumY = card.Y + 76;
                DrawText(graphics, data.AverageRating.ToString("0.0"), new RectangleF(card.X + 25, bigNumY, 90, 46), 38, TextColor, FontStyle.Bold);

                int roundedStars = (int)Math.Round(data.AverageRating);
                string starLine = new string('★', Math.Clamp(roundedStars, 0, 5)) + new string('☆', 5 - Math.Clamp(roundedStars, 0, 5));
                DrawText(graphics, starLine, new RectangleF(card.X + 108, bigNumY + 2, card.Width - 133, 22), 15, StarColor, FontStyle.Bold);

                int maxBucket = Math.Max(1, data.RatingDistribution.Count > 0 ? data.RatingDistribution.Max(b => b.Count) : 1);
                float barsY = bigNumY + 58;
                for (int i = 0; i < data.RatingDistribution.Count; i++)
                {
                    var bucket = data.RatingDistribution[i];
                    float y = barsY + i * 21;

                    DrawText(graphics, $"{bucket.Stars} ★", new RectangleF(card.X + 25, y, 34, 18), 10.5F, LightTextColor, FontStyle.Bold, StringAlignment.Far);
                    DrawProgress(graphics, new RectangleF(card.X + 66, y + 3, card.Width - 66 - 50, 8), bucket.Count / (float)maxBucket, StarColor);
                    DrawText(graphics, bucket.Count.ToString(), new RectangleF(card.Right - 34, y, 25, 18), 10.5F, LightTextColor, FontStyle.Bold);
                }

                if (!string.IsNullOrWhiteSpace(data.RecentFeedbackMessage))
                {
                    float quoteY = barsY + data.RatingDistribution.Count * 21 + 14;
                    using var dividerPen = new Pen(BorderColor);
                    graphics.DrawLine(dividerPen, card.X + 25, quoteY, card.Right - 25, quoteY);

                    string quote = $"“{data.RecentFeedbackMessage}” — {data.RecentFeedbackCustomer}";
                    var quoteBounds = new RectangleF(card.X + 25, quoteY + 10, card.Width - 50, card.Bottom - (quoteY + 10) - 10);
                    using var quoteFont = new Font("Segoe UI", 12, FontStyle.Italic, GraphicsUnit.Pixel);
                    using var quoteBrush = new SolidBrush(MutedColor);
                    using var quoteFormat = new StringFormat { Trimming = StringTrimming.EllipsisCharacter };
                    graphics.DrawString(quote, quoteFont, quoteBrush, quoteBounds, quoteFormat);
                }
            }

            private static void DrawApprovalBanner(Graphics graphics, RectangleF bounds, int pendingCount)
            {
                FillRounded(graphics, bounds, BannerColor, 12);
                StrokeRounded(graphics, bounds, ColorTranslator.FromHtml("#F2D99C"), 12);

                using var bellBrush = new SolidBrush(ColorTranslator.FromHtml("#F5B43F"));
                float bellX = bounds.X + 24;
                float bellY = bounds.Y + 23;
                graphics.FillEllipse(bellBrush, bellX + 3, bellY, 10, 12);
                graphics.FillRectangle(bellBrush, bellX + 2, bellY + 7, 12, 6);
                graphics.FillEllipse(bellBrush, bellX + 6, bellY + 13, 4, 3);

                string message = pendingCount == 1
                    ? "1 promotion is waiting on your approval."
                    : $"{pendingCount} promotions are waiting on your approval.";

                DrawText(graphics, message, new RectangleF(bounds.X + 46, bounds.Y + 12, bounds.Width - 205, 34), 16, TextColor);
            }

            private void DrawRevenueCard(Graphics graphics, RectangleF card, List<RevenuePointDto> trend, bool hovered, int hoveredIndex)
            {
                DrawCard(graphics, card);
                if (hovered)
                    StrokeRounded(graphics, RectangleF.Inflate(card, -0.5F, -0.5F), AccentColor, 14);
                DrawCardHeading(graphics, card, "Completed service value trend", $"Repaired, cancelled, and unrepairable tickets · {_rangeLabel}");
                DrawText(graphics, hovered ? "View full report →" : "", new RectangleF(card.Right - 175, card.Y + 22, 150, 20), 11, AccentColor, FontStyle.Bold, StringAlignment.Far);

                _trendData = trend;
                if (trend.Count < 2)
                {
                    DrawEmptyMessage(graphics, card, "Not enough data yet.");
                    _trendPoints = Array.Empty<PointF>();
                    return;
                }

                var plot = new RectangleF(card.X + 65, card.Y + 119, card.Width - 103, 102);
                _trendPlotBounds = plot;

                decimal maxValue = trend.Max(p => p.Amount);
                float maximum = maxValue > 0 ? (float)(maxValue * 1.15m) : 100F;

                var points = new PointF[trend.Count];
                for (int i = 0; i < trend.Count; i++)
                {
                    points[i] = new PointF(
                        plot.X + plot.Width * i / (trend.Count - 1),
                        plot.Bottom - (float)trend[i].Amount / maximum * plot.Height);
                }
                _trendPoints = points;

                var area = new PointF[points.Length + 2];
                area[0] = new PointF(plot.Left, plot.Bottom);
                Array.Copy(points, 0, area, 1, points.Length);
                area[area.Length - 1] = new PointF(plot.Right, plot.Bottom);

                using var areaBrush = new SolidBrush(ColorTranslator.FromHtml("#DDF2FA"));
                graphics.FillPolygon(areaBrush, area);

                using var gridPen = new Pen(ColorTranslator.FromHtml("#E5ECF2"), 1F);
                for (int i = 0; i < 3; i++)
                {
                    float y = plot.Y + plot.Height * i / 2F;
                    graphics.DrawLine(gridPen, plot.Left, y, plot.Right, y);

                    decimal label = maxValue > 0 ? maxValue * 1.15m * (2 - i) / 2 : 0;
                    DrawText(graphics, FormatPesoCompact(label), new RectangleF(plot.Left - 55, y - 9, 49, 18), 9, LightTextColor, FontStyle.Regular, StringAlignment.Far);
                }

                using var linePen = new Pen(AccentColor, 2F) { LineJoin = LineJoin.Round };
                graphics.DrawLines(linePen, points);

                PointF last = points[^1];
                using var pointBrush = new SolidBrush(AccentColor);
                graphics.FillEllipse(pointBrush, last.X - 4, last.Y - 4, 8, 8);

                DrawText(graphics, FormatPesoCompact(trend[^1].Amount), new RectangleF(last.X - 87, last.Y - 23, 81, 18), 10, AccentColor, FontStyle.Bold, StringAlignment.Far);

                int step = Math.Max(1, trend.Count / 7);
                for (int i = 0; i < trend.Count; i += step)
                {
                    bool today = i == trend.Count - 1;
                    string dayLabel = today ? "Today" : trend[i].Date.ToString("ddd");

                    DrawText(graphics, dayLabel, new RectangleF(points[i].X - 20, plot.Bottom + 6, 40, 20), 9,
                        today ? AccentColor : LightTextColor, today ? FontStyle.Bold : FontStyle.Regular, StringAlignment.Center);
                }

                // Hover tooltip — a crosshair at the nearest day plus a small
                // callout with its exact date and amount, since the chart
                // otherwise only ever labels the very last point.
                if (hoveredIndex >= 0 && hoveredIndex < points.Length)
                {
                    PointF hoverPoint = points[hoveredIndex];

                    using (var crosshairPen = new Pen(ColorTranslator.FromHtml("#BFD9E8"), 1F) { DashStyle = DashStyle.Dash })
                        graphics.DrawLine(crosshairPen, hoverPoint.X, plot.Top, hoverPoint.X, plot.Bottom);

                    using (var ringPen = new Pen(AccentColor, 2F))
                    {
                        graphics.FillEllipse(Brushes.White, hoverPoint.X - 5, hoverPoint.Y - 5, 10, 10);
                        graphics.DrawEllipse(ringPen, hoverPoint.X - 5, hoverPoint.Y - 5, 10, 10);
                    }

                    string dateLabel = trend[hoveredIndex].Date.ToString("MMM d");
                    string amountLabel = FormatPeso(trend[hoveredIndex].Amount);
                    float tipWidth = Math.Max(TextRenderer.MeasureText(dateLabel, new Font("Segoe UI", 9)).Width,
                        TextRenderer.MeasureText(amountLabel, new Font("Segoe UI", 11, FontStyle.Bold)).Width) + 24;
                    const float tipHeight = 46;

                    float tipX = Math.Clamp(hoverPoint.X - tipWidth / 2, plot.Left, plot.Right - tipWidth);
                    float tipY = hoverPoint.Y - tipHeight - 14;
                    if (tipY < card.Y + 12) tipY = hoverPoint.Y + 14;

                    var tipRect = new RectangleF(tipX, tipY, tipWidth, tipHeight);
                    using (var shadowBrush = new SolidBrush(Color.FromArgb(18, 0, 0, 0)))
                        graphics.FillPath(shadowBrush, RoundedPath(new RectangleF(tipRect.X + 1, tipRect.Y + 2, tipRect.Width, tipRect.Height), 8));
                    FillRounded(graphics, tipRect, ColorTranslator.FromHtml("#1C2B3A"), 8);

                    DrawText(graphics, dateLabel, new RectangleF(tipRect.X, tipRect.Y + 6, tipRect.Width, 16), 9, Color.FromArgb(200, 255, 255, 255), FontStyle.Regular, StringAlignment.Center);
                    DrawText(graphics, amountLabel, new RectangleF(tipRect.X, tipRect.Y + 22, tipRect.Width, 20), 12, Color.White, FontStyle.Bold, StringAlignment.Center);
                }
            }

            private static readonly Dictionary<string, Color> PaymentColors = new()
            {
                ["Paid"] = ColorTranslator.FromHtml("#178451"),
                ["Partial"] = ColorTranslator.FromHtml("#A1740D"),
                ["Unpaid"] = ColorTranslator.FromHtml("#AD2C31")
            };
            private static readonly Color DefaultPaymentColor = ColorTranslator.FromHtml("#6B8099");

            /// <summary>A real ring chart (concentric arcs, not a bar) — drawn as
            /// a thick stroked circle, one arc per payment status, starting from
            /// the top like a clock.</summary>
            private static void DrawPaymentCard(Graphics graphics, RectangleF card, List<PaymentBreakdownEntryDto> breakdown, string rangeLabel)
            {
                DrawCard(graphics, card);
                DrawCardHeading(graphics, card, "Payment status", $"Share of ticket value · {rangeLabel}");

                if (breakdown.Count == 0)
                {
                    DrawEmptyMessage(graphics, card, "No tickets yet.");
                    return;
                }

                decimal total = breakdown.Sum(b => b.Amount);

                const float ringSize = 96;
                var ringRect = new RectangleF(card.X + 25, card.Y + 80, ringSize, ringSize);
                const float ringThickness = 14;
                var strokeRect = RectangleF.Inflate(ringRect, -ringThickness / 2, -ringThickness / 2);

                using (var trackPen = new Pen(ColorTranslator.FromHtml("#EDF2F5"), ringThickness))
                    graphics.DrawEllipse(trackPen, strokeRect);

                float startAngle = -90;
                foreach (var entry in breakdown)
                {
                    float fraction = total > 0 ? (float)(entry.Amount / total) : 0;
                    if (fraction <= 0) continue;
                    float sweep = fraction * 360F;
                    Color color = PaymentColors.TryGetValue(entry.PaymentStatus, out var c) ? c : DefaultPaymentColor;

                    using var arcPen = new Pen(color, ringThickness) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                    // A hairline gap between segments so adjoining colors don't bleed together.
                    graphics.DrawArc(arcPen, strokeRect, startAngle + 1.5F, Math.Max(0, sweep - 3F));
                    startAngle += sweep;
                }

                float legendX = ringRect.Right + 24;
                float legendWidth = card.Right - 25 - legendX;
                for (int i = 0; i < breakdown.Count && i < 4; i++)
                {
                    var entry = breakdown[i];
                    Color color = PaymentColors.TryGetValue(entry.PaymentStatus, out var c) ? c : DefaultPaymentColor;
                    float y = card.Y + 82 + i * 28;

                    using var dotBrush = new SolidBrush(color);
                    graphics.FillEllipse(dotBrush, legendX, y + 5, 9, 9);

                    DrawText(graphics, entry.PaymentStatus, new RectangleF(legendX + 16, y, legendWidth * 0.45F, 20), 12.5F, MutedColor, FontStyle.Regular);
                    DrawText(graphics, FormatPesoCompact(entry.Amount), new RectangleF(legendX + legendWidth - legendWidth * 0.55F, y, legendWidth * 0.55F, 20), 12.5F, TextColor, FontStyle.Bold, StringAlignment.Far);
                }
            }

            private static readonly string[] PipelineOrder = { "Diagnose", "AwaitingApproval", "Repairing" };
            private static readonly Color[] PipelineColors =
            {
                ColorTranslator.FromHtml("#7DC7E7"),
                ColorTranslator.FromHtml("#36A9D4"),
                ColorTranslator.FromHtml("#107CA8")
            };

            private static readonly string[] PipelineShortNames = { "Diagnose", "Awaiting Appr.", "Repairing" };

            /// <summary>A left-to-right flow (big number, label, colored
            /// underline, arrow to the next stage) instead of a bar-per-stage
            /// list — reads as a pipeline, not just a ranked comparison.</summary>
            private static void DrawPipelineCard(Graphics graphics, RectangleF card, List<PipelineEntryDto> pipeline)
            {
                DrawCard(graphics, card);
                DrawCardHeading(graphics, card, "Ticket pipeline", "Where active tickets sit right now");

                var counts = PipelineOrder.Select(status => pipeline.FirstOrDefault(p => p.Status == status)?.Count ?? 0).ToArray();

                if (counts.Sum() == 0)
                {
                    DrawEmptyMessage(graphics, card, "No active tickets right now.");
                    return;
                }

                const float arrowWidth = 22;
                float stageWidth = (card.Width - 50 - arrowWidth * (PipelineOrder.Length - 1)) / PipelineOrder.Length;
                float y = card.Y + 90;

                for (int i = 0; i < PipelineOrder.Length; i++)
                {
                    float x = card.X + 25 + i * (stageWidth + arrowWidth);
                    var stage = new RectangleF(x, y, stageWidth, 110);

                    DrawText(graphics, counts[i].ToString(), new RectangleF(stage.X, stage.Y, stage.Width, 40), 26, TextColor, FontStyle.Bold, StringAlignment.Center);
                    DrawText(graphics, PipelineShortNames[i], new RectangleF(stage.X, stage.Y + 40, stage.Width, 34), 10.5F, LightTextColor, FontStyle.Bold, StringAlignment.Center);

                    var pill = new RectangleF(stage.X + 6, stage.Y + 78, stage.Width - 12, 7);
                    FillRounded(graphics, pill, PipelineColors[i], 4);

                    if (i < PipelineOrder.Length - 1)
                        DrawText(graphics, "›", new RectangleF(x + stageWidth, y + 4, arrowWidth, 40), 20, ColorTranslator.FromHtml("#C3CDD6"), FontStyle.Bold, StringAlignment.Center);
                }
            }

            private static void DrawStaffCard(Graphics graphics, RectangleF card, List<StaffWorkloadEntryDto> workload)
            {
                DrawCard(graphics, card);
                DrawCardHeading(graphics, card, "Staff workload", "Active tickets assigned per staff");

                if (workload.Count == 0)
                {
                    DrawEmptyMessage(graphics, card, "No active tickets assigned yet.");
                    return;
                }

                var top = workload.Take(5).ToList();
                int maxCount = Math.Max(1, top.Max(s => s.Count));
                Color barColor = ColorTranslator.FromHtml("#0FA7DF");
                Color[] avatarColors =
                {
                    AccentColor, ColorTranslator.FromHtml("#966C0C"), ColorTranslator.FromHtml("#6642AF"),
                    ColorTranslator.FromHtml("#188250"), ColorTranslator.FromHtml("#A32D2D")
                };

                for (int i = 0; i < top.Count; i++)
                {
                    float y = card.Y + 77 + i * 33F;

                    var avatar = new RectangleF(card.X + 25, y + 1, 24, 24);
                    using (var avatarBrush = new SolidBrush(avatarColors[i % avatarColors.Length]))
                        graphics.FillEllipse(avatarBrush, avatar);
                    string initials = string.Concat(top[i].StaffName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Take(2).Select(p => char.ToUpperInvariant(p[0])));
                    DrawText(graphics, initials, avatar, 9.5F, Color.White, FontStyle.Bold, StringAlignment.Center);

                    DrawText(graphics, top[i].StaffName, new RectangleF(card.X + 57, y, 113, 25), 14, MutedColor);
                    DrawProgress(graphics, new RectangleF(card.X + 170, y + 4, card.Width - 245, 18), top[i].Count / (float)maxCount, barColor);
                    DrawText(graphics, top[i].Count.ToString(), new RectangleF(card.Right - 50, y, 25, 25), 16, TextColor, FontStyle.Bold, StringAlignment.Far);
                }
            }
        }

        // ----------------------------------------------------------
        // Review button (drawn to match the banner exactly)
        // ----------------------------------------------------------

        private sealed class ReviewButton : Button
        {
            private bool _hovering;

            public ReviewButton()
            {
                FlatStyle = FlatStyle.Flat;
                FlatAppearance.BorderSize = 0;
                BackColor = BannerColor;
                UseVisualStyleBackColor = false;
                Cursor = Cursors.Hand;

                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            }

            protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovering = true; Invalidate(); }
            protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hovering = false; Invalidate(); }
            protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
            protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics graphics = e.Graphics;
                graphics.Clear(BannerColor);

                float scale = PrepareGraphics(graphics, this);
                float width = ClientSize.Width / scale;
                float height = ClientSize.Height / scale;

                FillRounded(graphics, new RectangleF(0, 0, width - 1, height - 1),
                    _hovering ? ColorTranslator.FromHtml("#085876") : SidebarColor, 9);

                DrawText(graphics, Text, new RectangleF(0, 0, width, height - 1), 14, Color.White, FontStyle.Bold, StringAlignment.Center);

                if (Focused && ShowFocusCues)
                    StrokeRounded(graphics, new RectangleF(3, 3, width - 7, height - 7), Color.White, 6);
            }
        }

        // ----------------------------------------------------------
        // Date filter bar chips / Apply button
        // ----------------------------------------------------------

        /// <summary>A small pill-shaped toggle button used for the date filter
        /// bar's preset chips and its Apply button. IsActive=true paints it
        /// solid-accent (a selected preset, or the always-accent Apply button);
        /// otherwise it's an outlined chip that fills light on hover.</summary>
        private sealed class PillButton : Button
        {
            private bool _hovering;

            [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
            public bool IsActive { get; set; }

            public PillButton()
            {
                FlatStyle = FlatStyle.Flat;
                FlatAppearance.BorderSize = 0;
                UseVisualStyleBackColor = false;
                Cursor = Cursors.Hand;

                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            }

            protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovering = true; Invalidate(); }
            protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hovering = false; Invalidate(); }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics graphics = e.Graphics;
                graphics.Clear(BackgroundColor);

                float scale = PrepareGraphics(graphics, this);
                float width = ClientSize.Width / scale;
                float height = ClientSize.Height / scale;
                var bounds = new RectangleF(0, 0, width - 1, height - 1);
                float radius = height / 2F;

                Color fill = IsActive ? AccentColor : (_hovering ? ColorTranslator.FromHtml("#F1F5F8") : Color.White);
                Color textColor = IsActive ? Color.White : TextColor;

                FillRounded(graphics, bounds, fill, radius);
                if (!IsActive)
                    StrokeRounded(graphics, bounds, BorderColor, radius);

                DrawText(graphics, Text, new RectangleF(0, 0, width, height - 1), 11.5F, textColor, FontStyle.Bold, StringAlignment.Center);
            }
        }
    }
}
