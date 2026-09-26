using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS.winforms.Api;
using CRMS.winforms.Controllers;
using CRMS.winforms.Shared;
using RoundedPanel = CRMS.winforms.RoundedPanel;

namespace CRMS.winforms.ManagerView
{
    /// <summary>
    /// Manager's Dashboard page — personal, not shop-wide. Manager doesn't oversee
    /// other staff or the shop's revenue (that's Admin's dashboard); this is scoped
    /// to the logged-in Manager's own tickets and their own promotions.
    ///
    /// LAYOUT: cards are manually positioned/resized in ReflowCards (same pattern
    /// as AddCustomerDialog) instead of nested Dock/FlowLayoutPanel — much more
    /// predictable for a page whose card heights change with real data.
    /// </summary>
    public partial class ManagerDashboardControl : UserControl
    {
        private static readonly Color Accent = Color.FromArgb(10, 116, 163);
        private static readonly Color CardBorder = Color.FromArgb(227, 233, 236);
        private static readonly Color TitleColor = Color.FromArgb(28, 39, 51);
        private static readonly Color MutedColor = Color.FromArgb(91, 107, 122);
        private static readonly Color FaintColor = Color.FromArgb(138, 149, 161);
        private static readonly Color WarnBg = Color.FromArgb(255, 244, 214);
        private static readonly Color WarnBorder = Color.FromArgb(240, 223, 176);
        private static readonly Color WarnFg = Color.FromArgb(150, 108, 12);
        private static readonly Color TrackColor = Color.FromArgb(238, 242, 244);

        private const int ItemsPerPage = 4;
        private const int CardGap = 14;

        private readonly ManagerController _controller;

        private Panel _scroll = null!;
        private RoundedPanel _statCard = null!;
        private RoundedPanel _attnBanner = null!;
        private RoundedPanel _pipelineCard = null!;
        private RoundedPanel _promoBreakdownCard = null!;
        private RoundedPanel _recentCard = null!;

        private Label _attnLabel = null!;
        private LinkLabel _attnLink = null!;
        private Panel _pipelineBars = null!;
        private Panel _promoBreakdownBars = null!;
        private FlowLayoutPanel _promoList = null!;
        private Label _pageIndicator = null!;
        private Button _btnPrevPage = null!;
        private Button _btnNextPage = null!;
        private Label[] _statValueLabels = null!;

        private ManagerDashboardDto? _data;
        private int _currentPage;

        /// <summary>Fires when the attention banner's link is clicked — the host
        /// page navigates to Pending Customers.</summary>
        public event EventHandler? PendingRequested;

        public ManagerDashboardControl(ManagerController controller)
        {
            _controller = controller;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            BackColor = Theme.PageBg;
            Padding = new Padding(Theme.PageMarginX, Theme.PageMarginTop, Theme.PageMarginX, Theme.PageMarginTop);

            _scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Theme.PageBg };

            _statCard = BuildStatCard();
            _attnBanner = BuildAttentionBanner();
            _pipelineCard = BuildPipelineCard();
            _promoBreakdownCard = BuildPromoBreakdownCard();
            _recentCard = BuildRecentPromotionsCard();

            _scroll.Controls.Add(_statCard);
            _scroll.Controls.Add(_attnBanner);
            _scroll.Controls.Add(_pipelineCard);
            _scroll.Controls.Add(_promoBreakdownCard);
            _scroll.Controls.Add(_recentCard);

            _scroll.Resize += (s, e) => ReflowCards();

            Controls.Add(_scroll);
            Controls.Add(Theme.CreatePageHeader("Dashboard", "Your tickets and promotions, at a glance", showTitle: false));

            ResumeLayout(false);
        }

        /// <summary>Restacks every card vertically at the panel's current width —
        /// called on resize and whenever a card's own height changes after data loads.</summary>
        private void ReflowCards()
        {
            int available = _scroll.ClientSize.Width - 4;
            if (available <= 0) return;

            _scroll.SuspendLayout();

            _statCard.Width = available;
            _statCard.Location = new Point(0, 0);
            int y = _statCard.Bottom + CardGap;

            if (_attnBanner.Visible)
            {
                _attnBanner.Width = available;
                _attnBanner.Location = new Point(0, y);
                y = _attnBanner.Bottom + CardGap;
            }

            int gap = CardGap;
            int leftWidth = (int)((available - gap) * 0.55F);
            int rightWidth = available - gap - leftWidth;

            _pipelineCard.Width = leftWidth;
            _pipelineCard.Location = new Point(0, y);
            _promoBreakdownCard.Width = rightWidth;
            _promoBreakdownCard.Location = new Point(leftWidth + gap, y);
            y += Math.Max(_pipelineCard.Height, _promoBreakdownCard.Height) + CardGap;

            _recentCard.Width = available;
            _recentCard.Location = new Point(0, y);

            _scroll.ResumeLayout();
        }

        // ===================== Stat strip =====================

        private RoundedPanel BuildStatCard()
        {
            var card = new RoundedPanel { Height = 70, BackColor = Color.White, BorderColor = CardBorder, CornerRadius = 12 };
            string[] captions = { "MY ACTIVE TICKETS", "AWAITING CUSTOMER APPROVAL", "PROMOTIONS AWAITING ADMIN", "PROMOTIONS APPROVED (MTD)" };
            _statValueLabels = new Label[4];

            card.Resize += (s, e) => LayoutStatCard(card, captions);
            LayoutStatCard(card, captions);
            return card;
        }

        private void LayoutStatCard(RoundedPanel card, string[] captions)
        {
            card.Controls.Clear();
            if (card.Width <= 0) return;

            float sectionWidth = card.Width / 4F;
            for (int i = 0; i < 4; i++)
            {
                int x = (int)(i * sectionWidth) + 20;

                card.Controls.Add(new Label { Text = captions[i], Font = new Font("Segoe UI", 8.25F, FontStyle.Bold), ForeColor = FaintColor, AutoSize = true, Location = new Point(x, 14) });

                var valueLabel = new Label { Text = "0", Font = new Font("Segoe UI", 15F, FontStyle.Bold), ForeColor = TitleColor, AutoSize = true, Location = new Point(x, 34) };
                card.Controls.Add(valueLabel);
                _statValueLabels[i] = valueLabel;

                if (i > 0)
                {
                    int dividerX = (int)(i * sectionWidth);
                    var divider = new Panel { Location = new Point(dividerX, 12), Size = new Size(1, card.Height - 24), BackColor = CardBorder };
                    card.Controls.Add(divider);
                }
            }
        }

        private void UpdateStatCard(ManagerDashboardDto data)
        {
            _statValueLabels[0].Text = data.MyActiveTickets.ToString();
            _statValueLabels[1].Text = data.AwaitingCustomerApproval.ToString();
            _statValueLabels[2].Text = data.PromotionsAwaitingAdmin.ToString();
            _statValueLabels[3].Text = data.PromotionsApprovedMtd.ToString();

            _statValueLabels[1].ForeColor = data.AwaitingCustomerApproval > 0 ? WarnFg : TitleColor;
            _statValueLabels[2].ForeColor = data.PromotionsAwaitingAdmin > 0 ? WarnFg : TitleColor;
            _statValueLabels[3].ForeColor = Theme.StatusRepairedText;
        }

        // ===================== Attention banner =====================

        private RoundedPanel BuildAttentionBanner()
        {
            var banner = new RoundedPanel { Height = 52, BackColor = WarnBg, BorderColor = WarnBorder, CornerRadius = 10, Visible = false };

            _attnLabel = new Label { Font = new Font("Segoe UI", 9.5F), ForeColor = TitleColor, AutoSize = true, Location = new Point(18, 16) };
            banner.Controls.Add(_attnLabel);

            _attnLink = new LinkLabel { Text = "View Pending Customers", Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), AutoSize = true, LinkColor = Accent };
            _attnLink.Click += (s, e) => PendingRequested?.Invoke(this, EventArgs.Empty);
            banner.Controls.Add(_attnLink);
            banner.Resize += (s, e) => _attnLink.Location = new Point(banner.Width - _attnLink.PreferredWidth - 18, 16);

            return banner;
        }

        // ===================== My ticket pipeline =====================

        private RoundedPanel BuildPipelineCard()
        {
            var card = new RoundedPanel { Height = 190, BackColor = Color.White, BorderColor = CardBorder, CornerRadius = 12 };

            card.Controls.Add(new Label { Text = "My ticket pipeline", Font = new Font("Segoe UI", 11.5F, FontStyle.Bold), ForeColor = TitleColor, AutoSize = true, Location = new Point(20, 16) });
            card.Controls.Add(new Label { Text = "Where your own active tickets sit right now", Font = new Font("Segoe UI", 9F), ForeColor = FaintColor, AutoSize = true, Location = new Point(20, 40) });

            _pipelineBars = new Panel { Location = new Point(20, 72), Height = 110 };
            card.Controls.Add(_pipelineBars);
            card.Resize += (s, e) => _pipelineBars.Width = Math.Max(60, card.Width - 40);

            return card;
        }

        private void UpdatePipelineCard(ManagerDashboardDto data)
        {
            _pipelineBars.Controls.Clear();
            int barWidth = _pipelineBars.Width;

            (string Key, string Label, Color Color)[] stages =
            {
                ("Diagnose", "Diagnose", Color.FromArgb(125, 199, 231)),
                ("AwaitingApproval", "Awaiting Approval", Color.FromArgb(155, 127, 224)),
                ("Repairing", "Repairing", Accent)
            };

            int maxCount = Math.Max(1, data.MyTicketPipeline.Select(p => p.Count).DefaultIfEmpty(0).Max());
            int y = 0;

            foreach (var stage in stages)
            {
                int count = data.MyTicketPipeline.FirstOrDefault(p => p.Status == stage.Key)?.Count ?? 0;
                AddBarRow(_pipelineBars, y, stage.Label, count, maxCount, stage.Color, barWidth);
                y += 36;
            }
        }

        private void AddBarRow(Panel host, int y, string label, int count, int maxCount, Color color, int totalWidth)
        {
            host.Controls.Add(new Label { Text = label, Font = new Font("Segoe UI", 9.5F), ForeColor = MutedColor, AutoSize = true, Location = new Point(0, y + 3) });

            int trackX = 130, trackWidth = Math.Max(20, totalWidth - 130 - 34);
            var track = new Panel { Location = new Point(trackX, y), Size = new Size(trackWidth, 16), BackColor = TrackColor };
            Theme.ApplyRoundedRegion(track, 5);
            host.Controls.Add(track);

            int fillWidth = (int)(trackWidth * (count / (float)maxCount));
            if (fillWidth > 0)
            {
                var fill = new Panel { Location = Point.Empty, Size = new Size(fillWidth, 16), BackColor = color };
                Theme.ApplyRoundedRegion(fill, 5);
                track.Controls.Add(fill);
            }

            host.Controls.Add(new Label { Text = count.ToString(), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = TitleColor, AutoSize = true, Location = new Point(trackX + trackWidth + 8, y + 1) });
        }

        // ===================== My promotions breakdown =====================

        private RoundedPanel BuildPromoBreakdownCard()
        {
            var card = new RoundedPanel { Height = 190, BackColor = Color.White, BorderColor = CardBorder, CornerRadius = 12 };

            card.Controls.Add(new Label { Text = "My promotions", Font = new Font("Segoe UI", 11.5F, FontStyle.Bold), ForeColor = TitleColor, AutoSize = true, Location = new Point(20, 16) });
            card.Controls.Add(new Label { Text = "Status of everything you've submitted", Font = new Font("Segoe UI", 9F), ForeColor = FaintColor, AutoSize = true, Location = new Point(20, 40) });

            _promoBreakdownBars = new Panel { Location = new Point(20, 72), Height = 110 };
            card.Controls.Add(_promoBreakdownBars);
            card.Resize += (s, e) => _promoBreakdownBars.Width = Math.Max(60, card.Width - 40);

            return card;
        }

        private void UpdatePromoBreakdownCard(ManagerDashboardDto data)
        {
            _promoBreakdownBars.Controls.Clear();
            int cardWidth = _promoBreakdownBars.Width;

            (string Key, string Label, Color Bg, Color Fg)[] statuses =
            {
                ("Approved", "Approved", Theme.StatusRepairedBg, Theme.StatusRepairedText),
                ("PendingApproval", "Pending", Theme.StatusDiagnoseBg, Theme.StatusDiagnoseText),
                ("Rejected", "Rejected", Theme.StatusUnrepairableBg, Theme.StatusUnrepairableText),
                ("Draft", "Draft", Theme.StatusCancelledBg, Theme.StatusCancelledText)
            };

            int total = data.MyPromotionsBreakdown.Sum(p => p.Count);
            if (total == 0)
            {
                _promoBreakdownBars.Controls.Add(new Label { Text = "No promotions submitted yet.", Font = new Font("Segoe UI", 9.5F, FontStyle.Italic), ForeColor = FaintColor, AutoSize = true });
                return;
            }

            int y = 0;
            foreach (var status in statuses)
            {
                int count = data.MyPromotionsBreakdown.FirstOrDefault(p => p.Status == status.Key)?.Count ?? 0;
                if (count == 0) continue;

                var dot = new Panel { Location = new Point(0, y + 6), Size = new Size(9, 9), BackColor = status.Fg };
                Theme.ApplyRoundedRegion(dot, 5);
                _promoBreakdownBars.Controls.Add(dot);

                _promoBreakdownBars.Controls.Add(new Label { Text = status.Label, Font = new Font("Segoe UI", 9.5F), ForeColor = MutedColor, AutoSize = true, Location = new Point(16, y) });
                _promoBreakdownBars.Controls.Add(new Label { Text = count.ToString(), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = TitleColor, AutoSize = true, Location = new Point(cardWidth - 40, y) });

                y += 26;
            }
        }

        // ===================== Recent promotions (paginated) =====================

        private RoundedPanel BuildRecentPromotionsCard()
        {
            var card = new RoundedPanel { Height = 340, BackColor = Color.White, BorderColor = CardBorder, CornerRadius = 12 };

            card.Controls.Add(new Label { Text = "Recent promotions", Font = new Font("Segoe UI", 11.5F, FontStyle.Bold), ForeColor = TitleColor, AutoSize = true, Location = new Point(20, 16) });
            card.Controls.Add(new Label { Text = "Your last submissions and their outcome", Font = new Font("Segoe UI", 9F), ForeColor = FaintColor, AutoSize = true, Location = new Point(20, 40) });

            _promoList = new FlowLayoutPanel { Location = new Point(20, 70), Height = 226, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            card.Controls.Add(_promoList);

            var pager = new Panel { Location = new Point(20, 300), Height = 30 };
            _btnPrevPage = new Button { Text = "‹ Prev", Width = 74, Height = 28, FlatStyle = FlatStyle.Flat, Location = new Point(0, 1) };
            _btnPrevPage.FlatAppearance.BorderColor = CardBorder;
            _btnPrevPage.Click += (s, e) => { if (_currentPage > 0) { _currentPage--; RenderPromoList(); } };

            _btnNextPage = new Button { Text = "Next ›", Width = 74, Height = 28, FlatStyle = FlatStyle.Flat };
            _btnNextPage.FlatAppearance.BorderColor = CardBorder;
            _btnNextPage.Click += (s, e) => { _currentPage++; RenderPromoList(); };

            _pageIndicator = new Label { Font = new Font("Segoe UI", 9F), ForeColor = FaintColor, AutoSize = true, Location = new Point(0, 6) };

            pager.Controls.Add(_btnPrevPage);
            pager.Controls.Add(_pageIndicator);
            pager.Controls.Add(_btnNextPage);

            card.Controls.Add(pager);
            card.Resize += (s, e) =>
            {
                _promoList.Width = Math.Max(60, card.Width - 40);
                pager.Width = Math.Max(60, card.Width - 40);
                pager.Top = card.Height - 46;
                _btnNextPage.Location = new Point(pager.Width - _btnNextPage.Width, 1);
                _pageIndicator.Location = new Point(Math.Max(0, (pager.Width - _pageIndicator.PreferredWidth) / 2), 6);
            };

            return card;
        }

        private void RenderPromoList()
        {
            _promoList.Controls.Clear();
            if (_data is null) return;

            var promotions = _data.RecentPromotions;
            int totalPages = Math.Max(1, (int)Math.Ceiling(promotions.Count / (double)ItemsPerPage));
            _currentPage = Math.Clamp(_currentPage, 0, totalPages - 1);

            var pageItems = promotions.Skip(_currentPage * ItemsPerPage).Take(ItemsPerPage).ToList();

            if (pageItems.Count == 0)
            {
                _promoList.Controls.Add(new Label
                {
                    Text = "You haven't submitted any promotions yet.",
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Italic),
                    ForeColor = FaintColor,
                    AutoSize = true,
                    Margin = new Padding(0, 20, 0, 0)
                });
            }
            else
            {
                foreach (var promo in pageItems)
                    _promoList.Controls.Add(BuildPromoRow(promo));
            }

            _pageIndicator.Text = $"Page {_currentPage + 1} of {totalPages}";
            _btnPrevPage.Enabled = _currentPage > 0;
            _btnNextPage.Enabled = _currentPage < totalPages - 1;
        }

        private Control BuildPromoRow(PromotionSummaryEntryDto promo)
        {
            var row = new Panel { Width = Math.Max(60, _promoList.Width - 20), Height = 54, Margin = Padding.Empty };
            row.Paint += (s, e) =>
            {
                using var pen = new Pen(CardBorder, 1);
                e.Graphics.DrawLine(pen, 0, 0, row.Width, 0);
            };

            row.Controls.Add(new Label { Text = promo.Title, Font = new Font("Segoe UI", 9.75F, FontStyle.Bold), ForeColor = TitleColor, AutoSize = true, Location = new Point(0, 10) });

            string metaText = promo.Status == PromotionStatus.Draft
                ? $"Not submitted yet · {promo.Type}"
                : $"Submitted {promo.CreatedAt:MMM d} · {promo.Type}";
            row.Controls.Add(new Label { Text = metaText, Font = new Font("Segoe UI", 8.5F), ForeColor = FaintColor, AutoSize = true, Location = new Point(0, 29) });

            var (bg, fg, text) = promo.Status switch
            {
                PromotionStatus.Approved => (Theme.StatusRepairedBg, Theme.StatusRepairedText, "Approved"),
                PromotionStatus.PendingApproval => (Theme.StatusDiagnoseBg, Theme.StatusDiagnoseText, "Pending"),
                PromotionStatus.Rejected => (Theme.StatusUnrepairableBg, Theme.StatusUnrepairableText, "Rejected"),
                _ => (Theme.StatusCancelledBg, Theme.StatusCancelledText, "Draft")
            };

            var pill = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = fg,
                BackColor = bg,
                AutoSize = true,
                Padding = new Padding(10, 4, 10, 4),
            };
            Theme.ApplyRoundedRegion(pill, 10);
            row.Controls.Add(pill);
            void PositionPill() => pill.Location = new Point(row.Width - pill.Width - 10, 16);
            PositionPill();
            row.Resize += (s, e) => PositionPill();

            return row;
        }

        /// <summary>Reloads every number from the real API. Call on every page navigation.</summary>
        public async Task LoadData()
        {
            _data = await _controller.GetDashboardAsync();
            _currentPage = 0;

            if (_data is null)
            {
                _attnBanner.Visible = false;
                ReflowCards();
                return;
            }

            UpdateStatCard(_data);

            if (_data.AwaitingCustomerApproval > 0)
            {
                _attnLabel.Text = _data.AwaitingCustomerApproval == 1
                    ? "1 of your tickets is waiting on a customer decision before repair can start."
                    : $"{_data.AwaitingCustomerApproval} of your tickets are waiting on a customer decision before repair can start.";
                _attnBanner.Visible = true;
            }
            else
            {
                _attnBanner.Visible = false;
            }

            ReflowCards();
            UpdatePipelineCard(_data);
            UpdatePromoBreakdownCard(_data);
            RenderPromoList();
        }
    }
}
