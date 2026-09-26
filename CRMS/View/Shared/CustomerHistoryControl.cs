using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS.winforms.Controllers;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using RoundedPanel = CRMS.winforms.RoundedPanel;
using RoundedButton = CRMS.winforms.RoundedButton;
using RoundedTextBox = CRMS.winforms.RoundedTextBox;
using RoundedRect = CRMS.winforms.RoundedRect;

namespace CRMS.winforms.Shared
{
    /// <summary>
    /// Card-based list of completed/cancelled/unrepairable tickets, with search,
    /// outcome/payment/pickup filters, and per-card actions. Used by Staff,
    /// Admin, and Manager — only Admin's instance (allowArchive: true) gets the
    /// Archive button; everyone sees the same cards otherwise.
    /// </summary>
    public partial class CustomerHistoryControl : UserControl
    {
        private static readonly Color CardBorder = Color.FromArgb(227, 233, 236);
        private static readonly Color TitleColor = Color.FromArgb(28, 39, 51);
        private static readonly Color MutedColor = Color.FromArgb(91, 107, 122);
        private static readonly Color FaintColor = Color.FromArgb(138, 149, 161);

        private const string NotYet = "Not Yet";
        private const string PickedUp = "Picked Up";
        private static readonly string[] PickupOptions = { NotYet, PickedUp };
        private static readonly string[] PaymentOptions = { "Unpaid", "Partial", "Paid" };

        private static readonly Dictionary<string, (Color Bg, Color Fg)> PickupPillColors = new()
        {
            [NotYet] = (Theme.StatusDiagnoseBg, Theme.StatusDiagnoseText),
            [PickedUp] = (Theme.StatusRepairedBg, Theme.StatusRepairedText)
        };

        private static readonly Dictionary<string, (Color Bg, Color Fg)> PaymentPillColors = new()
        {
            ["Unpaid"] = (Theme.StatusUnrepairableBg, Theme.StatusUnrepairableText),
            ["Partial"] = (Theme.StatusDiagnoseBg, Theme.StatusDiagnoseText),
            ["Paid"] = (Theme.StatusRepairedBg, Theme.StatusRepairedText)
        };

        // The outcome a ticket landed on when it left Pending Customers.
        private static readonly Dictionary<string, (Color Bg, Color Fg)> OutcomePillColors = new()
        {
            [TicketStatus.Repaired] = (Theme.StatusRepairedBg, Theme.StatusRepairedText),
            [TicketStatus.Cancelled] = (Theme.StatusCancelledBg, Theme.StatusCancelledText),
            [TicketStatus.Unrepairable] = (Theme.StatusUnrepairableBg, Theme.StatusUnrepairableText)
        };

        private readonly bool _allowArchive;
        private List<TicketRecord> _allTickets = new();
        private List<TicketRecord> _filteredTickets = new();

        private const int ItemsPerPage = 3;
        private int _currentPage;
        private Panel _pagerPanel = null!;
        private Button _btnPrevPage = null!;
        private Button _btnNextPage = null!;
        private Label _pageIndicator = null!;
        private Panel _pagerLayoutPanel = null!;

        private RoundedTextBox txtSearch = null!;
        private ComboBox cboOutcomeFilter = null!;
        private ComboBox cboPaymentFilter = null!;
        private ComboBox cboPickupFilter = null!;
        private Label lblRecordCount = null!;
        private Panel _cardScroll = null!;
        private FlowLayoutPanel _cardList = null!;

        /// <summary>Set by the host page to actually persist a pickup change.
        /// Called with (ticketId, pickedUp); returns (Success, Message).</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<int, bool, Task<(bool Success, string Message)>>? OnPickupChanged { get; set; }

        /// <summary>Set by the host page to actually persist a payment status
        /// change. Called with (ticketId, paymentStatus, amountPaid — only
        /// non-null for "Partial"); returns (Success, Message).</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<int, string, decimal?, Task<(bool Success, string Message)>>? OnPaymentChanged { get; set; }

        /// <summary>Set by the host page to actually archive a ticket. Only called
        /// when this control was constructed with allowArchive: true.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<int, Task<(bool Success, string Message)>>? OnArchiveRequested { get; set; }

        /// <summary>Set by the host page to actually persist customer feedback.
        /// Called with (ticketId, rating 1-5, message); returns (Success, Message).</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<int, int, string, Task<(bool Success, string Message)>>? OnFeedbackSubmitted { get; set; }

        public CustomerHistoryControl(bool allowArchive = false)
        {
            _allowArchive = allowArchive;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            BackColor = Theme.PageBg;
            Padding = new Padding(Theme.PageMarginX, Theme.PageMarginTop, Theme.PageMarginX, Theme.PageMarginTop);

            var toolbar = BuildToolbar();

            _cardScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Theme.PageBg };
            _cardList = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Location = Point.Empty
            };
            _cardScroll.Controls.Add(_cardList);
            _cardScroll.Resize += (s, e) => SyncCardWidths();

            _pagerPanel = BuildPager();

            // A container so the Fill/Bottom docking between the card list and
            // the pager doesn't get tangled up with the page's own Top-docked
            // toolbar/header — cardScroll (Fill) added first, pager (Bottom)
            // added after so it's laid out first and claims the bottom strip.
            var listContainer = new Panel { Dock = DockStyle.Fill, BackColor = Theme.PageBg };
            listContainer.Controls.Add(_cardScroll);
            listContainer.Controls.Add(_pagerPanel);

            ShowEmptyState("No completed tickets yet.");

            var exportButton = new Button
            {
                Text = "⬇ Export PDF",
                Width = 128,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = TitleColor,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            exportButton.FlatAppearance.BorderSize = 1;
            exportButton.FlatAppearance.BorderColor = CardBorder;
            exportButton.FlatAppearance.MouseOverBackColor = Theme.HeaderBg;
            exportButton.Click += (s, e) => ExportListToPdf();

            // Fill area first, toolbar/header on top — docked-Top children stack in
            // reverse add order (same load-bearing convention used across this app).
            Controls.Add(listContainer);
            Controls.Add(toolbar);
            Controls.Add(Theme.CreateSpacer(12));
            Controls.Add(Theme.CreatePageHeader("Customer History", "Completed, cancelled, and unrepairable service requests", exportButton, showTitle: false, showSubtitle: false));

            ResumeLayout(false);
        }

        private Panel BuildPager()
        {
            var pager = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = Theme.PageBg };

            _btnPrevPage = new Button { Text = "‹ Prev", Width = 84, Height = 32, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            _btnPrevPage.FlatAppearance.BorderColor = CardBorder;
            _btnPrevPage.Click += (s, e) => { if (_currentPage > 0) { _currentPage--; RenderCurrentPage(); } };

            _btnNextPage = new Button { Text = "Next ›", Width = 84, Height = 32, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            _btnNextPage.FlatAppearance.BorderColor = CardBorder;
            _btnNextPage.Click += (s, e) => { _currentPage++; RenderCurrentPage(); };

            _pageIndicator = new Label { Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = FaintColor, AutoSize = true, TextAlign = ContentAlignment.MiddleCenter };

            _pagerLayoutPanel = pager;
            pager.Resize += (s, e) => LayoutPager();

            pager.Controls.Add(_btnPrevPage);
            pager.Controls.Add(_pageIndicator);
            pager.Controls.Add(_btnNextPage);
            LayoutPager();

            return pager;
        }

        // Pulled out of BuildPager so RenderCurrentPage can call it again after
        // the page indicator's text changes ("Page 1 of 3" etc.) — its width
        // wasn't known yet the one time this used to run automatically (on
        // pager resize), so the Next button ended up positioned as if the
        // indicator were still empty, pushing it out from under the visible
        // group entirely on a page with more than one page of results.
        private void LayoutPager()
        {
            const int gap = 28;
            int groupWidth = _btnPrevPage.Width + gap + _pageIndicator.PreferredWidth + gap + _btnNextPage.Width;
            int startX = Math.Max(0, (_pagerLayoutPanel.Width - groupWidth) / 2);

            _btnPrevPage.Location = new Point(startX, (_pagerLayoutPanel.Height - _btnPrevPage.Height) / 2);
            _pageIndicator.Location = new Point(_btnPrevPage.Right + gap, (_pagerLayoutPanel.Height - _pageIndicator.Height) / 2);
            _btnNextPage.Location = new Point(_pageIndicator.Right + gap, (_pagerLayoutPanel.Height - _btnNextPage.Height) / 2);
        }

        private Panel BuildToolbar()
        {
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 76, BackColor = Theme.PageBg, Padding = new Padding(0, 0, 0, 12) };

            txtSearch = new RoundedTextBox
            {
                Location = new Point(0, 0),
                Size = new Size(320, 40),
                Placeholder = "Search customer, device, or ticket...",
                TextSize = 10F
            };
            txtSearch.ValueChanged += (s, e) => ApplyFilters();
            toolbar.Controls.Add(txtSearch);

            ComboBox BuildFilter(int x, string caption, string[] options)
            {
                toolbar.Controls.Add(new Label
                {
                    Text = caption,
                    Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                    ForeColor = FaintColor,
                    AutoSize = true,
                    Location = new Point(x, 0)
                });
                var combo = new ComboBox
                {
                    Location = new Point(x, 18),
                    Size = new Size(150, 40),
                    Font = new Font("Segoe UI", 9.5F),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    FlatStyle = FlatStyle.Flat
                };
                combo.Items.AddRange(options);
                combo.SelectedIndex = 0;
                combo.SelectedIndexChanged += (s, e) => ApplyFilters();
                toolbar.Controls.Add(combo);
                return combo;
            }

            cboOutcomeFilter = BuildFilter(336, "OUTCOME", new[] { "All outcomes", TicketStatus.Repaired, TicketStatus.Cancelled, TicketStatus.Unrepairable });
            cboPaymentFilter = BuildFilter(500, "PAYMENT", new[] { "All payments", "Paid", "Partial", "Unpaid" });
            cboPickupFilter = BuildFilter(664, "PICKUP", new[] { "All pickups", PickedUp, NotYet });

            lblRecordCount = new Label
            {
                Font = new Font("Segoe UI", 9F),
                ForeColor = FaintColor,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(toolbar.Width - 120, 30)
            };
            toolbar.Controls.Add(lblRecordCount);
            toolbar.Resize += (s, e) => lblRecordCount.Location = new Point(toolbar.Width - lblRecordCount.PreferredWidth, 30);

            return toolbar;
        }

        // Cards never compress below this — a narrower window scrolls
        // horizontally instead of squeezing columns until their text overlaps.
        private const int CardMinWidth = 1180;

        private void SyncCardWidths()
        {
            int width = Math.Max(CardMinWidth, _cardScroll.ClientSize.Width - 4);
            _cardList.Width = width;
            foreach (Control card in _cardList.Controls)
                card.Width = width - 4;
        }

        /// <summary>Reloads the list from the given ticket history (call on every page navigation).</summary>
        public void LoadData(IReadOnlyList<TicketRecord> history)
        {
            _allTickets = history.ToList();
            ApplyFilters();
        }

        // Remembers the filter inputs from the last real ApplyFilters() run, so
        // a spurious re-fire of a filter control's change event (a ComboBox or
        // text box can raise one without the user actually picking a new value —
        // e.g. on a focus change) is detected and ignored instead of blindly
        // resetting the pager back to page 1 out from under the user.
        private (string Search, string Outcome, string Payment, string Pickup)? _lastAppliedFilters;

        private void ApplyFilters()
        {
            string search = txtSearch.Value.Trim();
            string outcome = cboOutcomeFilter.SelectedItem?.ToString() ?? "All outcomes";
            string payment = cboPaymentFilter.SelectedItem?.ToString() ?? "All payments";
            string pickup = cboPickupFilter.SelectedItem?.ToString() ?? "All pickups";

            var current = (search, outcome, payment, pickup);
            bool filtersActuallyChanged = _lastAppliedFilters != current;
            _lastAppliedFilters = current;

            _filteredTickets = _allTickets.Where(t =>
                (string.IsNullOrEmpty(search) ||
                    t.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    t.DeviceName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    t.TicketId.ToString().Contains(search)) &&
                (outcome == "All outcomes" || t.Status == outcome) &&
                (payment == "All payments" || t.PaymentStatus == payment) &&
                (pickup == "All pickups" || (pickup == PickedUp) == t.PickedUpAt.HasValue))
                .ToList();

            if (filtersActuallyChanged)
                _currentPage = 0;
            RenderCurrentPage();

            lblRecordCount.Text = _allTickets.Count == _filteredTickets.Count
                ? $"{_filteredTickets.Count} record{(_filteredTickets.Count == 1 ? "" : "s")}"
                : $"Showing {_filteredTickets.Count} of {_allTickets.Count} records";
        }

        // Shows ItemsPerPage tickets at a time from _filteredTickets, with a
        // Prev/Next pager — a long history list otherwise turns into one huge
        // scroll of cards, which is exactly what the card redesign was trying
        // to get away from.
        private void RenderCurrentPage()
        {
            int totalPages = Math.Max(1, (int)Math.Ceiling(_filteredTickets.Count / (double)ItemsPerPage));
            _currentPage = Math.Clamp(_currentPage, 0, totalPages - 1);
            var pageItems = _filteredTickets.Skip(_currentPage * ItemsPerPage).Take(ItemsPerPage).ToList();

            _cardList.SuspendLayout();
            _cardList.Controls.Clear();

            if (_filteredTickets.Count == 0)
            {
                ShowEmptyState(_allTickets.Count == 0 ? "No completed tickets yet." : "No records match your search or filters.");
            }
            else
            {
                foreach (var ticket in pageItems)
                    _cardList.Controls.Add(BuildCard(ticket));
            }

            _cardList.ResumeLayout();
            SyncCardWidths();

            _pagerPanel.Visible = _filteredTickets.Count > ItemsPerPage;
            _pageIndicator.Text = $"Page {_currentPage + 1} of {totalPages}";
            _btnPrevPage.Enabled = _currentPage > 0;
            _btnNextPage.Enabled = _currentPage < totalPages - 1;
            LayoutPager();
        }

        private void ShowEmptyState(string message)
        {
            _cardList.Controls.Clear();
            _cardList.Controls.Add(new Label
            {
                Text = message,
                Font = new Font("Segoe UI", 10F, FontStyle.Italic),
                ForeColor = FaintColor,
                AutoSize = true,
                Margin = new Padding(4, 20, 0, 0)
            });
        }

        /// <summary>Turns PaymentStatus + AmountPaid into one readable line, so
        /// "Partial" always shows the actual remaining balance instead of just
        /// the word "Partial".</summary>
        private static string FormatBalance(string paymentStatus, decimal amountPaid, decimal estimatedCost) => paymentStatus switch
        {
            "Paid" => "Paid in full",
            "Partial" => $"{amountPaid:₱#,##0.00} of {estimatedCost:₱#,##0.00} paid ({estimatedCost - amountPaid:₱#,##0.00} left)",
            _ => $"{estimatedCost:₱#,##0.00} due"
        };

        private static void StyleStatusCombo(ComboBox combo, Dictionary<string, (Color Bg, Color Fg)> colorMap)
        {
            combo.FlatStyle = FlatStyle.Flat;
            combo.DrawMode = DrawMode.OwnerDrawFixed;
            combo.ItemHeight = 24;
            combo.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            combo.DrawItem += (s, e) =>
            {
                // Index < 0 is the closed box itself (the "selected value" view) —
                // color it the same as the matching dropdown row so the combo reads
                // as a colored status pill even before it's opened, not just inside
                // the popup list.
                string text = e.Index >= 0 ? combo.Items[e.Index]?.ToString() ?? string.Empty : combo.Text;
                var (bg, fg) = colorMap.TryGetValue(text, out var colors) ? colors : (Color.White, Theme.TextPrimary);

                using var brush = new SolidBrush(bg);
                e.Graphics.FillRectangle(brush, e.Bounds);
                TextRenderer.DrawText(e.Graphics, text, combo.Font, e.Bounds, fg,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);
            };
        }

        // Fixed-width right rail for the action buttons; everything else splits
        // the remaining width proportionally so the card always fills its space
        // instead of leaving a fixed-offset gap on wide screens. The card itself
        // never gets narrower than CardMinWidth (the scroll area handles that by
        // scrolling sideways instead), so these columns always have enough real
        // pixels — every field is still drawn inside its own bounded rectangle
        // with ellipsis trimming as a last-resort safety net, never raw
        // unbounded text, so two columns can never visually merge into one.
        private const int ActionsRailWidth = 192;
        private static readonly float[] ColumnWeights = { 0.24f, 0.12f, 0.14f, 0.11f, 0.24f, 0.15f };
        private const int CardHeight = 226;
        private const int TopRowY = 66;
        private const int ValueRowY = 86;
        private const int ColumnGap = 20;

        private static readonly Font BadgeFont = new("Segoe UI", 11.5F, FontStyle.Bold);
        private static readonly Font NameFont = new("Segoe UI", 15F, FontStyle.Bold);
        private static readonly Font DeviceFont = new("Segoe UI", 11.5F);
        private static readonly Font CaptionFont = new("Segoe UI", 8.75F, FontStyle.Bold);
        private static readonly Font ValueFont = new("Segoe UI", 12F, FontStyle.Bold);
        private static readonly Font BalanceFont = new("Segoe UI", 10.5F, FontStyle.Bold);
        private static readonly Font DiscountNoteFont = new("Segoe UI", 7.75F, FontStyle.Bold);
        private static readonly Font PillFont = new("Segoe UI", 10F, FontStyle.Bold);
        private static readonly Color DividerColor = Color.FromArgb(235, 239, 243);

        private Control BuildCard(TicketRecord ticket)
        {
            var card = new TicketCardPanel(ticket, _allowArchive)
            {
                Margin = new Padding(0, 0, 0, 14),
                Padding = new Padding(22, 18, 22, 18)
            };

            bool suppressEvents = false;

            card.CboPickup.SelectedIndexChanged += async (s, e) =>
            {
                if (suppressEvents || OnPickupChanged is null) return;
                string newValue = card.CboPickup.SelectedItem?.ToString() ?? NotYet;
                bool pickedUp = newValue == PickedUp;

                var (success, message) = await OnPickupChanged(ticket.TicketId, pickedUp);
                if (!success)
                {
                    MessageBox.Show(FindForm(), $"Could not update pickup status:\n{message}", "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    suppressEvents = true;
                    card.CboPickup.SelectedItem = pickedUp ? NotYet : PickedUp;
                    suppressEvents = false;
                }
                else
                {
                    ticket.PickedUpAt = pickedUp ? (ticket.PickedUpAt ?? DateTime.Now) : null;

                    // The customer is right there handing the device back — ask
                    // for feedback now instead of hoping someone remembers to add
                    // it later. Skipping the popup is fine (Cancel just leaves it
                    // unset, editable anytime from the "+ Add Feedback" prompt).
                    if (pickedUp && OnFeedbackSubmitted != null && ticket.FeedbackRating is null)
                        await PromptForFeedback(ticket, card);
                }
            };

            card.LblFeedback.Click += async (s, e) => await PromptForFeedback(ticket, card);

            card.CboPayment.SelectedIndexChanged += async (s, e) =>
            {
                if (suppressEvents || OnPaymentChanged is null) return;

                string newValue = card.CboPayment.SelectedItem?.ToString() ?? "Unpaid";
                string previousValue = ticket.PaymentStatus;

                decimal? amountToSend = null;
                if (newValue == "Partial")
                {
                    using var amountDialog = new AmountInputDialog(ticket.EstimatedCost, ticket.AmountPaid);
                    if (amountDialog.ShowDialog(FindForm()) != DialogResult.OK)
                    {
                        suppressEvents = true;
                        card.CboPayment.SelectedItem = previousValue;
                        suppressEvents = false;
                        return;
                    }
                    amountToSend = amountDialog.AmountValue;
                }

                var (success, message) = await OnPaymentChanged(ticket.TicketId, newValue, amountToSend);
                if (!success)
                {
                    MessageBox.Show(FindForm(), $"Could not update payment status:\n{message}", "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    suppressEvents = true;
                    card.CboPayment.SelectedItem = previousValue;
                    suppressEvents = false;
                }
                else
                {
                    ticket.PaymentStatus = newValue;
                    ticket.AmountPaid = newValue switch
                    {
                        "Paid" => ticket.EstimatedCost,
                        "Partial" => amountToSend!.Value,
                        _ => 0m
                    };
                    if (newValue == "Paid")
                        card.CboPayment.Enabled = false;
                    card.Invalidate();
                }
            };

            card.BtnDownload.Click += (s, e) => DownloadPdf(ticket);
            if (card.BtnArchive != null)
                card.BtnArchive.Click += async (s, e) => await ArchiveTicket(ticket, card);

            return card;
        }

        /// <summary>
        /// A ticket's history card. All of its field text and the outcome badge
        /// are drawn directly in OnPaint (like a real design system's card, not a
        /// pile of AutoSize Labels) — that sidesteps the truncation bug an
        /// AutoSize Label + rounded-region clip combo used to cause here, and
        /// lets the whole card scale cleanly to any width. Only the pieces that
        /// need to be interactive (Payment/Pickup dropdowns, action buttons) are
        /// real child controls, positioned to match the painted columns.
        /// </summary>
        private sealed class TicketCardPanel : RoundedPanel
        {
            private const int ActionButtonWidth = 176;
            private const int ActionButtonHeight = 42;

            private readonly TicketRecord _ticket;
            private readonly bool _allowArchive;

            public ComboBox CboPayment { get; }
            public ComboBox CboPickup { get; }
            public RoundedButton BtnDownload { get; }
            public RoundedButton? BtnArchive { get; }
            public Label LblFeedback { get; }

            public TicketCardPanel(TicketRecord ticket, bool allowArchive)
            {
                _ticket = ticket;
                _allowArchive = allowArchive;

                Height = CardHeight;
                BackColor = Color.White;
                BorderColor = CardBorder;
                CornerRadius = 12;
                DoubleBuffered = true;

                CboPayment = new ComboBox { Size = new Size(170, 40), DropDownStyle = ComboBoxStyle.DropDownList };
                CboPayment.Items.AddRange(PaymentOptions);
                CboPayment.SelectedItem = ticket.PaymentStatus;
                // Once fully paid, that's a closed financial fact — lock the
                // control so it can't be walked back to Partial/Unpaid by
                // mistake (the API enforces this too, this just avoids the
                // pointless round trip).
                CboPayment.Enabled = ticket.PaymentStatus != "Paid";
                StyleStatusCombo(CboPayment, PaymentPillColors);
                Controls.Add(CboPayment);

                CboPickup = new ComboBox { Size = new Size(170, 40), DropDownStyle = ComboBoxStyle.DropDownList };
                CboPickup.Items.AddRange(PickupOptions);
                CboPickup.SelectedItem = ticket.PickedUpAt.HasValue ? PickedUp : NotYet;
                StyleStatusCombo(CboPickup, PickupPillColors);
                Controls.Add(CboPickup);
                CboPickup.SelectedIndexChanged += (s, e) => Invalidate();
                CboPayment.SelectedIndexChanged += (s, e) => Invalidate();

                LblFeedback = new Label
                {
                    AutoSize = true,
                    Cursor = Cursors.Hand,
                    Font = new Font("Segoe UI", 13F)
                };
                Controls.Add(LblFeedback);
                RefreshFeedback();

                BtnDownload = new RoundedButton
                {
                    Text = "⬇  Download PDF",
                    Size = new Size(ActionButtonWidth, ActionButtonHeight),
                    CornerRadius = 8,
                    BackColor = Theme.Accent,
                    HoverColor = Theme.AccentHover,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
                };
                Controls.Add(BtnDownload);

                if (allowArchive)
                {
                    BtnArchive = new RoundedButton
                    {
                        Text = "🗑  Archive",
                        Size = new Size(ActionButtonWidth, ActionButtonHeight),
                        CornerRadius = 8,
                        BackColor = Color.White,
                        HoverColor = Color.FromArgb(245, 246, 248),
                        ForeColor = TitleColor,
                        Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
                    };
                    Controls.Add(BtnArchive);
                }

                LayoutControls();
            }

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                LayoutControls();
            }

            /// <summary>Shows the recorded rating as stars, or a plain "add
            /// feedback" prompt when none exists yet — called once at construction
            /// and again after a feedback dialog is saved.</summary>
            public void RefreshFeedback()
            {
                if (_ticket.FeedbackRating is int rating and >= 1 and <= 5)
                {
                    LblFeedback.Text = new string('★', rating) + new string('☆', 5 - rating);
                    LblFeedback.ForeColor = Color.FromArgb(250, 176, 5);
                }
                else
                {
                    LblFeedback.Text = "+ Add Feedback";
                    LblFeedback.ForeColor = Theme.Accent;
                }
            }

            // Returns each column's start x (relative to the inner content area)
            // plus that column's own width, so every drawn field can be bounded
            // to a real rectangle instead of drawn as unbounded text — the fix
            // for columns visually merging into each other when the card is
            // narrower than the content wants.
            private (int[] X, int[] Width) ComputeColumns(out int innerWidth)
            {
                innerWidth = Math.Max(760, ClientSize.Width - Padding.Left - Padding.Right - ActionsRailWidth - 24);
                int x = 0;
                var colX = new int[ColumnWeights.Length];
                var colW = new int[ColumnWeights.Length];
                for (int i = 0; i < ColumnWeights.Length; i++)
                {
                    colX[i] = x;
                    colW[i] = (int)(innerWidth * ColumnWeights[i]);
                    x += colW[i];
                }
                return (colX, colW);
            }

            private void LayoutControls()
            {
                // Setting Height/Width in the constructor (before the child
                // controls exist yet) fires OnResize immediately — bail out until
                // everything referenced below has actually been created.
                if (CboPayment is null || CboPickup is null || BtnDownload is null || LblFeedback is null)
                    return;

                var (colX, _) = ComputeColumns(out _);
                int left = Padding.Left;

                CboPayment.Location = new Point(left + colX[4], Padding.Top + ValueRowY + 32);
                CboPickup.Location = new Point(left + colX[5], Padding.Top + ValueRowY + 32);
                LblFeedback.Location = new Point(left + colX[5], Padding.Top + TopRowY);

                int actionX = ClientSize.Width - Padding.Right - ActionButtonWidth;
                if (_allowArchive && BtnArchive != null)
                {
                    BtnDownload.Location = new Point(actionX, 40);
                    BtnArchive.Location = new Point(actionX, 40 + ActionButtonHeight + 12);
                }
                else
                {
                    BtnDownload.Location = new Point(actionX, 86);
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                var (colX, colW) = ComputeColumns(out _);
                int left = Padding.Left;
                int top = Padding.Top;

                // Small ticket-number tag, separate from the Customer column so
                // the customer name lines up on the same baseline as every other
                // column's value instead of floating on its own row above them.
                var badgeRect = new Rectangle(left, top, 76, 30);
                using (var badgeBrush = new SolidBrush(Theme.Accent))
                using (var badgePath = RoundedRect.Path(badgeRect, 8))
                    g.FillPath(badgeBrush, badgePath);
                TextRenderer.DrawText(g, $"#{_ticket.TicketId:0000}", BadgeFont, badgeRect, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                // Every field below — Customer included — is drawn inside its own
                // bounded rectangle (colW[col] wide, minus a gap) with
                // end-ellipsis trimming as the last-resort fallback, and every
                // column's caption/value pair starts at the same TopRowY/
                // ValueRowY baseline, so the whole row reads as one aligned line
                // instead of the customer name floating above the rest.
                void Field(int col, string caption, string value, int yOffset = 0, Font? valueFont = null, int valueHeight = 26)
                {
                    int x = left + colX[col];
                    int w = Math.Max(60, colW[col] - ColumnGap);
                    TextRenderer.DrawText(g, caption, CaptionFont, new Rectangle(x, top + TopRowY + yOffset, w, 18), FaintColor,
                        TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                    TextRenderer.DrawText(g, value, valueFont ?? ValueFont, new Rectangle(x, top + ValueRowY + yOffset, w, valueHeight), TitleColor,
                        TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                }

                // The customer name uses a taller font than every other value, so
                // it needs a taller box (a cramped box is what was clipping the
                // middle of tall glyphs like "g"/"j" before) and the Device row
                // underneath needs extra clearance to not run into it.
                Field(0, "CUSTOMER", _ticket.FullName, valueFont: NameFont, valueHeight: 34);
                Field(0, "DEVICE", _ticket.DeviceName, 56, DeviceFont);
                string handledBy = string.IsNullOrWhiteSpace(_ticket.StaffName) ? "-" : _ticket.StaffName;
                if (!string.IsNullOrWhiteSpace(_ticket.BranchName))
                    handledBy += $" · {_ticket.BranchName}";
                Field(1, "HANDLED BY", handledBy);
                Field(2, "CREATED", _ticket.CreatedAt.ToString("MMM d, yyyy"));
                Field(2, "CLOSED", _ticket.CompletedAt?.ToString("MMM d, yyyy") ?? "-", 44);
                Field(3, "CHARGE", _ticket.EstimatedCost.ToString("₱#,##0.00"));
                if (_ticket.OriginalPrice.HasValue && _ticket.DiscountAmount is > 0)
                {
                    int chargeX = left + colX[3];
                    int chargeW = Math.Max(60, colW[3] - ColumnGap);
                    TextRenderer.DrawText(g, $"was {_ticket.OriginalPrice:₱#,##0.00} · −{_ticket.DiscountAmount:₱#,##0.00}", DiscountNoteFont,
                        new Rectangle(chargeX, top + ValueRowY + 22, chargeW, 16), Theme.StatusRepairedText,
                        TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                }

                var balanceColor = _ticket.PaymentStatus == "Paid" ? Theme.StatusRepairedText
                    : _ticket.PaymentStatus == "Partial" ? Theme.StatusDiagnoseText
                    : Theme.StatusUnrepairableText;
                int balX = left + colX[4];
                int balW = Math.Max(120, colW[4] - ColumnGap);
                TextRenderer.DrawText(g, "BALANCE", CaptionFont, new Rectangle(balX, top + TopRowY, balW, 18), FaintColor,
                    TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(g, FormatBalance(_ticket.PaymentStatus, _ticket.AmountPaid, _ticket.EstimatedCost), BalanceFont,
                    new Rectangle(balX, top + ValueRowY, balW, 22), balanceColor,
                    TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                // Outcome status badge — sized from measured text every paint (and
                // capped to the column's own width), so it can never clip.
                var (bg, fg) = OutcomePillColors.TryGetValue(_ticket.Status, out var oc) ? oc : (Theme.StatusCancelledBg, Theme.StatusCancelledText);
                var textSize = TextRenderer.MeasureText(g, _ticket.Status, PillFont, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPrefix);
                int pillMaxW = Math.Max(70, colW[1] - ColumnGap);
                int pillW = Math.Min(textSize.Width + 34, pillMaxW);
                const int pillH = 30;
                int pillX = left + colX[1];
                int pillY = top + ValueRowY + 28;
                var pillRect = new Rectangle(pillX, pillY, pillW, pillH);
                using (var pillBrush = new SolidBrush(bg))
                using (var pillPath = RoundedRect.Path(pillRect, pillH / 2))
                    g.FillPath(pillBrush, pillPath);
                using (var dotBrush = new SolidBrush(fg))
                    g.FillEllipse(dotBrush, pillX + 11, pillY + pillH / 2 - 4, 8, 8);
                TextRenderer.DrawText(g, _ticket.Status, PillFont, new Rectangle(pillX + 24, pillY, pillW - 28, pillH), fg,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                using var dividerPen = new Pen(DividerColor);
                int dividerTop = top - 8;
                int dividerBottom = Math.Min(top + ValueRowY + 68, ClientSize.Height - Padding.Bottom + 6);
                for (int i = 1; i < colX.Length; i++)
                {
                    int x = left + colX[i] - ColumnGap / 2;
                    g.DrawLine(dividerPen, x, dividerTop, x, dividerBottom);
                }
            }
        }

        private async Task PromptForFeedback(TicketRecord ticket, TicketCardPanel card)
        {
            if (OnFeedbackSubmitted is null) return;

            using var dialog = new FeedbackDialog(ticket.FullName, ticket.FeedbackRating, ticket.FeedbackMessage);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

            var (success, message) = await OnFeedbackSubmitted(ticket.TicketId, dialog.RatingValue, dialog.MessageValue);
            if (!success)
            {
                MessageBox.Show(FindForm(), $"Could not save the feedback:\n{message}", "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ticket.FeedbackRating = dialog.RatingValue;
            ticket.FeedbackMessage = dialog.MessageValue;
            card.RefreshFeedback();
        }

        private async Task ArchiveTicket(TicketRecord ticket, Control card)
        {
            if (OnArchiveRequested is null) return;

            var confirm = MessageBox.Show(FindForm(),
                $"Archive {ticket.FullName}'s ticket? It will be hidden from Customer History, but the record is kept.",
                "Archive Ticket", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            var (success, message) = await OnArchiveRequested(ticket.TicketId);
            if (!success)
            {
                MessageBox.Show(FindForm(), $"Could not archive the ticket:\n{message}", "Archive Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _allTickets.Remove(ticket);
            ApplyFilters();
        }

        /// <summary>Exports exactly the tickets currently visible under the
        /// active search/outcome/payment/pickup filters — every matching
        /// ticket, not just whichever page the on-screen pager happens to be
        /// showing (pagination is a browsing convenience, not a filter).</summary>
        private void ExportListToPdf()
        {
            if (_filteredTickets.Count == 0)
            {
                MessageBox.Show(FindForm(), "There's nothing to export with the current filters.",
                    "Nothing to Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Title = "Save Customer History PDF",
                Filter = "PDF Document|*.pdf",
                FileName = $"Customer-History-{DateTime.Now:yyyy-MM-dd}.pdf"
            };
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

            var rows = _filteredTickets.Select(t => new[]
            {
                $"#{t.TicketId:0000}",
                t.FullName,
                t.DeviceName,
                t.Status,
                t.CompletedAt?.ToString("MMM d, yyyy") ?? "-",
                t.PaymentStatus,
                t.EstimatedCost.ToString("₱#,##0.00")
            }).ToList();

            try
            {
                PdfExportService.ExportTable(
                    "Customer History", $"{_filteredTickets.Count} matching record{(_filteredTickets.Count == 1 ? "" : "s")}",
                    new[] { "Ticket #", "Customer", "Device", "Outcome", "Completed", "Payment", "Amount" },
                    rows, dialog.FileName);
                MessageBox.Show(this, "PDF saved successfully.", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not save the PDF:\n{ex.Message}", "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DownloadPdf(TicketRecord ticket)
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Save Customer History PDF",
                Filter = "PDF Document|*.pdf",
                FileName = $"Ticket-{ticket.TicketId}-{ticket.FullName.Replace(' ', '_')}.pdf"
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                PdfExportService.ExportTicketHistory(ticket, dialog.FileName);
                MessageBox.Show(this, "PDF saved successfully.", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not save the PDF:\n{ex.Message}", "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
