using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS.winforms.Api;
using CRMS.winforms.Controllers;
using CRMS.winforms.Shared;
using RoundedPanel = CRMS.winforms.RoundedPanel;
using RoundedRect = CRMS.winforms.RoundedRect;

namespace CRMS.Winforms.Admin
{
    /// <summary>
    /// Real Admin Reports page — three sections, every row a live aggregate over
    /// this tenant's own tickets (no sample data): Monthly Sales (click a month
    /// to drill into the tickets behind it, via the same KpiDetailDialog the
    /// Dashboard's KPI tiles use), Top Customers (repeat-visit/total-spend
    /// ranking — the "buying behavior" view for a repair shop), and Most Common
    /// Devices/Repair Concerns.
    /// </summary>
    public partial class AnalyticReportControl : UserControl
    {
        private readonly AdminController _controller;

        private DataGridView _salesGrid = null!;
        private DataGridView _customersGrid = null!;
        private DataGridView _devicesGrid = null!;
        private DataGridView _concernsGrid = null!;
        private DataGridView _feedbackGrid = null!;
        private DataGridView _promotionsGrid = null!;
        private Label _salesEmptyLabel = null!;
        private Label _devicesEmptyLabel = null!;
        private Label _concernsEmptyLabel = null!;
        private Label _feedbackEmptyLabel = null!;
        private Label _promotionsEmptyLabel = null!;
        private int _devicesMaxCount = 1;
        private int _concernsMaxCount = 1;

        // The full (unpaged) dataset behind each section, kept around so its
        // Export PDF button can export everything currently loaded/filtered —
        // not just whichever page the on-screen pager happens to be showing.
        private List<MonthlySalesEntryDto> _salesData = new();
        private List<TopCustomerEntryDto> _customersData = new();
        private List<CommonValueEntryDto> _devicesData = new();
        private List<CommonValueEntryDto> _concernsData = new();
        private List<FeedbackEntryDto> _feedbackData = new();
        private List<PromotionEffectivenessEntryDto> _promotionsData = new();
        private PromotionComparisonResponseDto? _promotionComparisonData;

        private TablePager<MonthlySalesEntryDto> _salesPager = null!;
        private TablePager<TopCustomerEntryDto> _customersPager = null!;
        private TablePager<CommonValueEntryDto> _devicesPager = null!;
        private TablePager<CommonValueEntryDto> _concernsPager = null!;
        private TablePager<FeedbackEntryDto> _feedbackPager = null!;
        private TablePager<PromotionEffectivenessEntryDto> _promotionsPager = null!;

        private Control _salesSection = null!;
        private Control _customersSection = null!;
        private Control _commonSection = null!;
        private Control _feedbackSection = null!;
        private Control _promotionsSection = null!;

        private ComparisonCardRefs _withPromoCard = null!;
        private ComparisonCardRefs _withoutPromoCard = null!;
        private Label _promoDeltaBadge = null!;
        private Label _promoDeltaLabel = null!;

        /// <summary>Section keys accepted by <see cref="ShowOnly"/>.</summary>
        public const string SectionSales = "sales";
        public const string SectionCustomers = "customers";
        public const string SectionCommon = "common";
        public const string SectionFeedback = "feedback";
        public const string SectionPromotions = "promotions";

        public AnalyticReportControl(AdminController controller)
        {
            _controller = controller;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            BackColor = Theme.PageBg;
            Padding = new Padding(Theme.PageMarginX, Theme.PageMarginTop, Theme.PageMarginX, Theme.PageMarginTop);

            // Each section fills the whole page (not a fixed-width block) since
            // only one is ever visible at a time now — the table should use all
            // the room the window actually has, not a fixed 1120px card with
            // dead space around it.
            _salesSection = BuildMonthlySalesSection(out _salesGrid, out _salesEmptyLabel);
            _customersSection = BuildTopCustomersSection(out _customersGrid);
            _commonSection = BuildCommonValuesSection(out _devicesGrid, out _concernsGrid);
            _feedbackSection = BuildFeedbackSection(out _feedbackGrid, out _feedbackEmptyLabel);
            _promotionsSection = BuildPromotionEffectivenessSection(out _promotionsGrid, out _promotionsEmptyLabel);

            _salesSection.Dock = DockStyle.Fill;
            _customersSection.Dock = DockStyle.Fill;
            _commonSection.Dock = DockStyle.Fill;
            _feedbackSection.Dock = DockStyle.Fill;
            _promotionsSection.Dock = DockStyle.Fill;

            var exportFullReportButton = BuildPrimaryButton("⬇ Export Full Report");
            exportFullReportButton.Click += (s, e) => ExportFullReport();

            Controls.Add(_salesSection);
            Controls.Add(_customersSection);
            Controls.Add(_commonSection);
            Controls.Add(_feedbackSection);
            Controls.Add(_promotionsSection);
            Controls.Add(Theme.CreateSpacer(12));
            Controls.Add(Theme.CreatePageHeader("Reports", "Live business insights", exportFullReportButton, showTitle: false, showSubtitle: false));

            ResumeLayout(false);

            ShowOnly(SectionSales);
            _ = LoadDataAsync();
        }

        /// <summary>Shows exactly one report section at a time — the sidebar's
        /// Reports dropdown picks which, so a user only ever sees the one table
        /// they asked for instead of scrolling past the other two.</summary>
        public void ShowOnly(string section)
        {
            _salesSection.Visible = section == SectionSales;
            _customersSection.Visible = section == SectionCustomers;
            _commonSection.Visible = section == SectionCommon;
            _feedbackSection.Visible = section == SectionFeedback;
            _promotionsSection.Visible = section == SectionPromotions;
        }

        // Same light-blue grid line the Promotions page uses, so every table in
        // the app reads as one consistent design instead of Reports having its
        // own separate style.
        private static readonly Color GridLine = Color.FromArgb(183, 228, 248);

        private static Panel BuildSectionHeader(string title, string subtitle, Action? onExport = null)
        {
            var panel = new Panel { Dock = DockStyle.Top, Height = 52 };
            panel.Controls.Add(new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(0, 0)
            });
            panel.Controls.Add(new Label
            {
                Text = subtitle,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(0, 28)
            });

            if (onExport != null)
            {
                var exportButton = BuildOutlineButton("⬇ Export PDF");
                exportButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                exportButton.Click += (s, e) => onExport();
                panel.Controls.Add(exportButton);

                void Reposition() => exportButton.Location = new Point(panel.Width - exportButton.Width, (52 - exportButton.Height) / 2);
                panel.Resize += (s, e) => Reposition();
                Reposition();
            }

            return panel;
        }

        /// <summary>A light outline button — used for every "Export PDF" action
        /// on a report section, secondary to the page-level "Export Full Report"
        /// button below.</summary>
        private static Button BuildOutlineButton(string text)
        {
            var button = new Button
            {
                Text = text,
                Width = 128,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = GridLine;
            button.FlatAppearance.MouseOverBackColor = Theme.HeaderBg;
            return button;
        }

        /// <summary>The filled-accent primary button — used for Admin's page-level
        /// "Export Full Report" action, matching the "+ Add" button style used
        /// elsewhere in the app.</summary>
        private static Button BuildPrimaryButton(string text)
        {
            var button = new Button
            {
                Text = text,
                Width = 172,
                Height = 38,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Accent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Theme.AccentHover;
            return button;
        }

        /// <summary>The same card shell the Promotions table uses: a white
        /// RoundedPanel with a light-blue border, not a plain square-cornered
        /// panel — this is what "look the same as Promotions" turns into.</summary>
        private static RoundedPanel CreateStrongCard()
        {
            return new RoundedPanel
            {
                BackColor = Color.White,
                BorderColor = GridLine,
                CornerRadius = 12,
                Padding = new Padding(10)
            };
        }

        /// <summary>Bigger text, taller rows, and the same light-blue grid lines
        /// as the Promotions table, so every DataGridView in the app reads as
        /// one consistent design.</summary>
        private static void StyleReportGrid(DataGridView grid)
        {
            grid.Font = new Font("Segoe UI", 10.5F);
            grid.ForeColor = Theme.TextPrimary;
            grid.RowTemplate.Height = 52;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            grid.GridColor = GridLine;

            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Theme.HeaderBg,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(18, 0, 18, 0)
            };

            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 10.5F),
                Padding = new Padding(18, 0, 18, 0),
                SelectionBackColor = Theme.SelectionTint,
                SelectionForeColor = Theme.TextPrimary
            };

            grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Theme.RowAlt,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 10.5F),
                Padding = new Padding(18, 0, 18, 0),
                SelectionBackColor = Theme.SelectionTint,
                SelectionForeColor = Theme.TextPrimary
            };
        }

        private Control BuildMonthlySalesSection(out DataGridView grid, out Label emptyLabel)
        {
            var wrapper = new Panel { Dock = DockStyle.Fill, BackColor = Theme.PageBg };
            var header = BuildSectionHeader("Monthly Sales", "Last 12 months of resolved-ticket revenue — click a row to see the tickets behind it", ExportSalesReport);

            var card = CreateStrongCard();
            card.Dock = DockStyle.Fill;

            grid = Theme.CreateStyledGrid();
            StyleReportGrid(grid);
            grid.ReadOnly = true;
            grid.Columns.Add("Month", "Month");
            grid.Columns.Add("Tickets", "Tickets Resolved");
            grid.Columns.Add("Revenue", "Revenue");
            grid.Columns[0].FillWeight = 40;
            grid.Columns[1].FillWeight = 30;
            grid.Columns[2].FillWeight = 30;
            grid.CellDoubleClick += SalesGrid_RowActivated;
            var localGrid = grid;
            grid.CellClick += (s, e) => { if (e.RowIndex >= 0) SalesGrid_RowActivated(localGrid, e); };
            grid.Cursor = Cursors.Hand;

            emptyLabel = new Label
            {
                Text = "No resolved tickets yet.",
                Font = new Font("Segoe UI", 10F, FontStyle.Italic),
                ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };

            _salesPager = new TablePager<MonthlySalesEntryDto>(6, (page, _) =>
            {
                localGrid.Rows.Clear();
                foreach (var entry in page)
                {
                    string label = new DateTime(entry.Year, entry.Month, 1).ToString("MMMM yyyy", CultureInfo.InvariantCulture);
                    int rowIndex = localGrid.Rows.Add(label, entry.TicketCount, entry.Revenue.ToString("₱#,##0.00"));
                    localGrid.Rows[rowIndex].Tag = (entry.Year, entry.Month);
                }
            });

            // Fill (grid) added first, then bottom pager, then header (Top) —
            // same reverse-add-order docking convention used throughout this app.
            card.Controls.Add(grid);
            card.Controls.Add(emptyLabel);
            card.Controls.Add(_salesPager.Bar);
            wrapper.Controls.Add(card);
            wrapper.Controls.Add(Theme.CreateSpacer(10));
            wrapper.Controls.Add(header);
            return wrapper;
        }

        private Control BuildTopCustomersSection(out DataGridView grid)
        {
            var wrapper = new Panel { Dock = DockStyle.Fill, BackColor = Theme.PageBg };
            var header = BuildSectionHeader("Top Customers", "Ranked by total spend — who keeps coming back, and how much they've paid", ExportCustomersReport);

            var card = CreateStrongCard();
            card.Dock = DockStyle.Fill;

            grid = Theme.CreateStyledGrid();
            StyleReportGrid(grid);
            grid.ReadOnly = true;
            grid.Columns.Add("Customer", "Customer");
            grid.Columns.Add("Visits", "Visits");
            grid.Columns.Add("TotalSpent", "Total Spent");
            grid.Columns.Add("LastVisit", "Last Visit");
            grid.Columns[0].FillWeight = 40;
            grid.Columns[1].FillWeight = 15;
            grid.Columns[2].FillWeight = 25;
            grid.Columns[3].FillWeight = 20;

            var localGrid = grid;
            _customersPager = new TablePager<TopCustomerEntryDto>(6, (page, _) =>
            {
                localGrid.Rows.Clear();
                foreach (var c in page)
                    localGrid.Rows.Add(c.FullName, c.VisitCount, c.TotalSpent.ToString("₱#,##0.00"), c.LastVisit.ToString("MMM d, yyyy"));
                if (page.Count == 0)
                    localGrid.Rows.Add("No resolved tickets yet.", "", "", "");
            });

            card.Controls.Add(grid);
            card.Controls.Add(_customersPager.Bar);
            wrapper.Controls.Add(card);
            wrapper.Controls.Add(Theme.CreateSpacer(10));
            wrapper.Controls.Add(header);
            return wrapper;
        }

        private static readonly Color StarColor = Color.FromArgb(250, 176, 5);
        private static readonly Font StarFont = new("Segoe UI", 11F);

        private Control BuildFeedbackSection(out DataGridView grid, out Label emptyLabel)
        {
            var wrapper = new Panel { Dock = DockStyle.Fill, BackColor = Theme.PageBg };
            var header = BuildSectionHeader("Customer Feedback", "What customers said when they picked up their device — entered by Staff, since customers don't have their own login", ExportFeedbackReport);

            var card = CreateStrongCard();
            card.Dock = DockStyle.Fill;

            grid = Theme.CreateStyledGrid();
            StyleReportGrid(grid);
            grid.ReadOnly = true;
            grid.Columns.Add("Customer", "Customer");
            grid.Columns.Add("Device", "Device");
            grid.Columns.Add("Rating", "Rating");
            grid.Columns.Add("Message", "Comment");
            grid.Columns.Add("Date", "Date");
            grid.Columns["Customer"].FillWeight = 20;
            grid.Columns["Device"].FillWeight = 18;
            grid.Columns["Rating"].FillWeight = 14;
            grid.Columns["Message"].FillWeight = 33;
            grid.Columns["Date"].FillWeight = 15;
            grid.Columns["Message"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.RowTemplate.Height = 64;

            // Paint the Rating cell as actual stars instead of a bare "4" — the
            // whole point of a star rating is that it reads at a glance.
            var localGrid = grid;
            localGrid.CellPainting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                if (localGrid.Columns[e.ColumnIndex].Name != "Rating") return;
                if (!int.TryParse(e.Value?.ToString(), out int rating)) return;

                e.PaintBackground(e.CellBounds, true);
                string stars = new string('★', rating) + new string('☆', 5 - rating);
                TextRenderer.DrawText(e.Graphics, stars, StarFont, e.CellBounds, StarColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                e.Handled = true;
            };

            _feedbackPager = new TablePager<FeedbackEntryDto>(6, (page, _) =>
            {
                localGrid.Rows.Clear();
                foreach (var f in page)
                    localGrid.Rows.Add(f.CustomerName, f.DeviceName, f.Rating, f.Message, f.CreatedAt.ToString("MMM d, yyyy"));
            });

            card.Controls.Add(localGrid);
            emptyLabel = BuildEmptyLabel("No feedback recorded yet.");
            card.Controls.Add(emptyLabel);
            card.Controls.Add(_feedbackPager.Bar);

            wrapper.Controls.Add(card);
            wrapper.Controls.Add(Theme.CreateSpacer(10));
            wrapper.Controls.Add(header);
            return wrapper;
        }

        /// <summary>The Labels inside one comparison card that need updating
        /// once real data loads — the card itself is built once, only its
        /// numbers change on refresh.</summary>
        private sealed class ComparisonCardRefs
        {
            public required Label BigValue;
            public required Label TicketsValue;
            public required Label RevenueValue;
            public required Label DiscountValue;
        }

        private static readonly Color PurpleBg = Color.FromArgb(233, 224, 250);
        private static readonly Color PurpleText = Color.FromArgb(102, 66, 175);

        private static (RoundedPanel Card, ComparisonCardRefs Refs) BuildComparisonCard(string tagText, Color tagBg, Color tagFg)
        {
            var card = new RoundedPanel { BackColor = Color.White, BorderColor = GridLine, CornerRadius = 14, Dock = DockStyle.Fill };

            var tag = new Label
            {
                Text = tagText,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = tagFg,
                BackColor = tagBg,
                AutoSize = true,
                Padding = new Padding(10, 4, 10, 4),
                Location = new Point(22, 20)
            };
            Theme.ApplyRoundedRegion(tag, 10);
            card.Controls.Add(tag);

            // A 22pt bold label's real rendered height (~42-44px) is taller than
            // its font size alone suggests — the caption below it was placed
            // only 36px down, so the peso value's own descenders overlapped and
            // clipped the "avg. ticket value" text sitting right under it.
            var bigValue = new Label { Font = new Font("Segoe UI", 22F, FontStyle.Bold), ForeColor = Theme.TextPrimary, AutoSize = true, Location = new Point(22, 56) };
            card.Controls.Add(bigValue);
            card.Controls.Add(new Label { Text = "avg. ticket value", Font = new Font("Segoe UI", 9F), ForeColor = Theme.TextSecondary, AutoSize = true, Location = new Point(22, 104) });

            (Label Caption, Label Value) AddStatRow(int y, string caption)
            {
                card.Controls.Add(new Label { Text = caption, Font = new Font("Segoe UI", 9.5F), ForeColor = Theme.TextSecondary, AutoSize = true, Location = new Point(22, y) });
                var value = new Label { Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Theme.TextPrimary, AutoSize = true, Location = new Point(180, y) };
                card.Controls.Add(value);
                return (default!, value);
            }

            var ticketsRow = AddStatRow(142, "Tickets");
            var revenueRow = AddStatRow(170, "Total revenue");
            var discountRow = AddStatRow(198, "Total discount given");

            return (card, new ComparisonCardRefs
            {
                BigValue = bigValue,
                TicketsValue = ticketsRow.Value,
                RevenueValue = revenueRow.Value,
                DiscountValue = discountRow.Value
            });
        }

        private Control BuildPromotionEffectivenessSection(out DataGridView grid, out Label emptyLabel)
        {
            var wrapper = new Panel { Dock = DockStyle.Fill, BackColor = Theme.PageBg };
            var header = BuildSectionHeader("Promotion Effectiveness", "Did offering a promotion actually change ticket value — compared against resolved tickets with no promotion applied", ExportPromotionsReport);

            var compareRow = new TableLayoutPanel { Dock = DockStyle.Top, Height = 248, ColumnCount = 3, RowCount = 1, Margin = new Padding(0, 0, 0, 16) };
            compareRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
            compareRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            compareRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
            compareRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var (withCard, withRefs) = BuildComparisonCard("🏷 PROMOTION APPLIED", PurpleBg, PurpleText);
            withCard.Margin = new Padding(0, 0, 10, 0);
            _withPromoCard = withRefs;

            var vsPanel = new Panel { Dock = DockStyle.Fill };
            _promoDeltaBadge = new Label
            {
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = Theme.StatusRepairedText,
                BackColor = Theme.StatusRepairedBg,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(58, 58),
                Anchor = AnchorStyles.None
            };
            Theme.ApplyRoundedRegion(_promoDeltaBadge, 29);
            _promoDeltaLabel = new Label
            {
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                TextAlign = ContentAlignment.TopCenter,
                Width = 110,
                Anchor = AnchorStyles.None
            };
            vsPanel.Controls.Add(_promoDeltaBadge);
            vsPanel.Controls.Add(_promoDeltaLabel);
            void LayoutVsPanel()
            {
                _promoDeltaBadge.Location = new Point((vsPanel.Width - _promoDeltaBadge.Width) / 2, vsPanel.Height / 2 - 50);
                _promoDeltaLabel.Location = new Point((vsPanel.Width - _promoDeltaLabel.Width) / 2, vsPanel.Height / 2 + 14);
            }
            vsPanel.Resize += (s, e) => LayoutVsPanel();

            var (withoutCard, withoutRefs) = BuildComparisonCard("NO PROMOTION", Theme.HeaderBg, Theme.TextSecondary);
            withoutCard.Margin = new Padding(10, 0, 0, 0);
            _withoutPromoCard = withoutRefs;

            compareRow.Controls.Add(withCard, 0, 0);
            compareRow.Controls.Add(vsPanel, 1, 0);
            compareRow.Controls.Add(withoutCard, 2, 0);
            compareRow.Resize += (s, e) => LayoutVsPanel();

            var card = CreateStrongCard();
            card.Dock = DockStyle.Fill;

            grid = Theme.CreateStyledGrid();
            StyleReportGrid(grid);
            grid.ReadOnly = true;
            grid.Columns.Add("Promotion", "Promotion");
            grid.Columns.Add("Type", "Type");
            grid.Columns.Add("Used", "Times Used");
            grid.Columns.Add("Discount", "Discount Given");
            grid.Columns.Add("Revenue", "Revenue Generated");
            grid.Columns.Add("Avg", "Avg. Ticket Value");
            grid.Columns["Promotion"].FillWeight = 26;
            grid.Columns["Type"].FillWeight = 16;
            grid.Columns["Used"].FillWeight = 12;
            grid.Columns["Discount"].FillWeight = 16;
            grid.Columns["Revenue"].FillWeight = 16;
            grid.Columns["Avg"].FillWeight = 14;

            var localGrid = grid;
            emptyLabel = BuildEmptyLabel("No approved promotions yet.");

            _promotionsPager = new TablePager<PromotionEffectivenessEntryDto>(6, (page, _) =>
            {
                localGrid.Rows.Clear();
                foreach (var p in page)
                    localGrid.Rows.Add(p.Title, p.Type, p.TimesUsed, $"−{p.DiscountGiven:₱#,##0.00}", p.RevenueGenerated.ToString("₱#,##0.00"), p.AvgTicketValue.ToString("₱#,##0.00"));
            });

            card.Controls.Add(localGrid);
            card.Controls.Add(emptyLabel);
            card.Controls.Add(_promotionsPager.Bar);

            wrapper.Controls.Add(card);
            wrapper.Controls.Add(Theme.CreateSpacer(10));
            wrapper.Controls.Add(compareRow);
            wrapper.Controls.Add(header);
            return wrapper;
        }

        private Control BuildCommonValuesSection(out DataGridView devicesGrid, out DataGridView concernsGrid)
        {
            var wrapper = new Panel { Dock = DockStyle.Fill, BackColor = Theme.PageBg };
            var header = BuildSectionHeader("Most Common Repairs", "Which device types and stated concerns show up most often", ExportCommonRepairsReport);

            var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            split.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var devicesCard = CreateStrongCard();
            devicesCard.Dock = DockStyle.Fill;
            devicesCard.Margin = new Padding(0, 0, 14, 0);
            devicesGrid = BuildRankedValueGrid("Device", () => _devicesMaxCount);
            var localDevicesGrid = devicesGrid;
            _devicesPager = new TablePager<CommonValueEntryDto>(5, (page, startIndex) =>
            {
                localDevicesGrid.Rows.Clear();
                for (int i = 0; i < page.Count; i++)
                {
                    int rowIndex = localDevicesGrid.Rows.Add("", page[i].Value, page[i].Count);
                    localDevicesGrid.Rows[rowIndex].Tag = startIndex + i + 1;
                }
            });
            devicesCard.Controls.Add(devicesGrid);
            _devicesEmptyLabel = BuildEmptyLabel("No tickets yet.");
            devicesCard.Controls.Add(_devicesEmptyLabel);
            devicesCard.Controls.Add(_devicesPager.Bar);

            var concernsCard = CreateStrongCard();
            concernsCard.Dock = DockStyle.Fill;
            concernsCard.Margin = new Padding(14, 0, 0, 0);
            concernsGrid = BuildRankedValueGrid("Reported Concern", () => _concernsMaxCount);
            var localConcernsGrid = concernsGrid;
            _concernsPager = new TablePager<CommonValueEntryDto>(5, (page, startIndex) =>
            {
                localConcernsGrid.Rows.Clear();
                for (int i = 0; i < page.Count; i++)
                {
                    int rowIndex = localConcernsGrid.Rows.Add("", page[i].Value, page[i].Count);
                    localConcernsGrid.Rows[rowIndex].Tag = startIndex + i + 1;
                }
            });
            concernsCard.Controls.Add(concernsGrid);
            _concernsEmptyLabel = BuildEmptyLabel("No tickets yet.");
            concernsCard.Controls.Add(_concernsEmptyLabel);
            concernsCard.Controls.Add(_concernsPager.Bar);

            split.Controls.Add(devicesCard, 0, 0);
            split.Controls.Add(concernsCard, 1, 0);

            wrapper.Controls.Add(split);
            wrapper.Controls.Add(Theme.CreateSpacer(10));
            wrapper.Controls.Add(header);
            return wrapper;
        }

        private static Label BuildEmptyLabel(string text) => new()
        {
            Text = text,
            Font = new Font("Segoe UI", 10F, FontStyle.Italic),
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Visible = false
        };

        /// <summary>Pages an in-memory list into a DataGridView a few rows at a
        /// time with a Prev/Next bar — every report table was one long
        /// scrollable list before, this keeps each page short and scannable.
        /// The bar hides itself entirely when everything already fits on one
        /// page, same as Customer History's pager.</summary>
        private sealed class TablePager<T>
        {
            private readonly int _pageSize;
            private readonly Action<List<T>, int> _renderPage;
            private List<T> _items = new();
            private int _page;

            public Panel Bar { get; }
            private readonly Button _prev;
            private readonly Button _next;
            private readonly Label _indicator;

            /// <summary>renderPage receives the current page's items plus the
            /// zero-based index (within the full list) that the first of those
            /// items sits at — needed by grids that show an absolute rank
            /// (e.g. Most Common Repairs), so page 2's badges read "#6, #7..."
            /// instead of restarting from "#1" every page.</summary>
            public TablePager(int pageSize, Action<List<T>, int> renderPage)
            {
                _pageSize = pageSize;
                _renderPage = renderPage;

                Bar = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = Color.White, Visible = false };

                _prev = new Button { Text = "‹ Prev", Width = 84, Height = 32, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
                _prev.FlatAppearance.BorderColor = GridLine;
                _prev.Click += (s, e) => { if (_page > 0) { _page--; Render(); } };

                _next = new Button { Text = "Next ›", Width = 84, Height = 32, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
                _next.FlatAppearance.BorderColor = GridLine;
                _next.Click += (s, e) => { _page++; Render(); };

                _indicator = new Label { Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Theme.TextSecondary, AutoSize = true };

                Bar.Controls.Add(_prev);
                Bar.Controls.Add(_indicator);
                Bar.Controls.Add(_next);
                Bar.Resize += (s, e) => Layout();
                Layout();
            }

            public void SetItems(List<T> items)
            {
                _items = items;
                _page = 0;
                Render();
            }

            private void Render()
            {
                int totalPages = Math.Max(1, (int)Math.Ceiling(_items.Count / (double)_pageSize));
                _page = Math.Clamp(_page, 0, totalPages - 1);
                int startIndex = _page * _pageSize;
                var pageItems = _items.Skip(startIndex).Take(_pageSize).ToList();

                _renderPage(pageItems, startIndex);

                Bar.Visible = _items.Count > _pageSize;
                _indicator.Text = $"Page {_page + 1} of {totalPages}";
                _prev.Enabled = _page > 0;
                _next.Enabled = _page < totalPages - 1;
                Layout();
            }

            private void Layout()
            {
                const int gap = 28;
                int groupWidth = _prev.Width + gap + _indicator.PreferredWidth + gap + _next.Width;
                int startX = Math.Max(0, (Bar.Width - groupWidth) / 2);

                _prev.Location = new Point(startX, (Bar.Height - _prev.Height) / 2);
                _indicator.Location = new Point(_prev.Right + gap, (Bar.Height - _indicator.Height) / 2);
                _next.Location = new Point(_indicator.Right + gap, (Bar.Height - _next.Height) / 2);
            }
        }

        /// <summary>A two-column grid (rank badge + name are painted, not plain
        /// text) with a proportional bar filling the count column — a small bar
        /// chart baked into the table instead of a bare number, so "most
        /// common" is something you see at a glance, not just read.</summary>
        private static DataGridView BuildRankedValueGrid(string valueHeader, Func<int> getMax)
        {
            var grid = Theme.CreateStyledGrid();
            StyleReportGrid(grid);
            grid.ReadOnly = true;
            grid.Columns.Add("Rank", "#");
            grid.Columns.Add("Value", valueHeader);
            grid.Columns.Add("Count", "Tickets");
            grid.Columns["Rank"].FillWeight = 14;
            grid.Columns["Value"].FillWeight = 56;
            grid.Columns["Count"].FillWeight = 30;
            grid.Columns["Rank"].SortMode = DataGridViewColumnSortMode.NotSortable;

            grid.CellPainting += (s, e) => PaintRankedValueCell(e, grid, getMax);
            return grid;
        }

        private static void PaintRankedValueCell(DataGridViewCellPaintingEventArgs e, DataGridView grid, Func<int> getMax)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string columnName = grid.Columns[e.ColumnIndex].Name;
            if (columnName != "Rank" && columnName != "Count") return;

            e.PaintBackground(e.CellBounds, true);
            e.Graphics!.SmoothingMode = SmoothingMode.AntiAlias;

            if (columnName == "Rank")
            {
                // The row's Tag carries its rank within the full (unpaged) list —
                // falls back to the in-page position only if something forgot to
                // set it, so a badge is never left blank.
                int rank = grid.Rows[e.RowIndex].Tag as int? ?? e.RowIndex + 1;
                var (bg, fg) = rank switch
                {
                    1 => (Color.FromArgb(255, 224, 130), Color.FromArgb(146, 100, 0)),
                    2 => (Color.FromArgb(226, 229, 233), Color.FromArgb(90, 98, 107)),
                    3 => (Color.FromArgb(233, 201, 173), Color.FromArgb(120, 74, 30)),
                    _ => (Theme.HeaderBg, Theme.TextSecondary)
                };

                const int size = 28;
                var circleRect = new Rectangle(e.CellBounds.X + (e.CellBounds.Width - size) / 2, e.CellBounds.Y + (e.CellBounds.Height - size) / 2, size, size);
                using (var brush = new SolidBrush(bg))
                    e.Graphics.FillEllipse(brush, circleRect);
                TextRenderer.DrawText(e.Graphics, rank.ToString(), new Font("Segoe UI", 9.5F, FontStyle.Bold), circleRect, fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
            else if (int.TryParse(e.Value?.ToString(), out int count))
            {
                // Number on the left, proportional bar filling the rest — kept
                // as two separate regions (not text overlaid on the bar) so the
                // number stays legible dark-on-white regardless of how far the
                // fill reaches.
                var numberArea = new Rectangle(e.CellBounds.X, e.CellBounds.Y, 34, e.CellBounds.Height);
                TextRenderer.DrawText(e.Graphics, count.ToString(), new Font("Segoe UI", 9.5F, FontStyle.Bold), numberArea, Theme.TextPrimary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                int max = Math.Max(1, getMax());
                var track = new Rectangle(e.CellBounds.X + 40, e.CellBounds.Y + (e.CellBounds.Height - 12) / 2, e.CellBounds.Width - 40 - 12, 12);
                if (track.Width > 0)
                {
                    using (var trackBrush = new SolidBrush(Theme.HeaderBg))
                    using (var trackPath = RoundedRect.Path(track, track.Height / 2))
                        e.Graphics.FillPath(trackBrush, trackPath);

                    int fillWidth = Math.Max(track.Height, Math.Min(track.Width, (int)(track.Width * (count / (double)max))));
                    var fillRect = new Rectangle(track.X, track.Y, fillWidth, track.Height);
                    using (var fillBrush = new SolidBrush(Theme.Accent))
                    using (var fillPath = RoundedRect.Path(fillRect, fillRect.Height / 2))
                        e.Graphics.FillPath(fillBrush, fillPath);
                }
            }

            e.Handled = true;
        }

        private async void SalesGrid_RowActivated(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || sender is not DataGridView grid) return;
            if (grid.Rows[e.RowIndex].Tag is not (int year, int month)) return;

            var detail = await _controller.GetMonthlySalesDetailAsync(year, month);
            if (detail is null)
            {
                MessageBox.Show(FindForm(), "Could not load that month's tickets.", "Load Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string monthLabel = new DateTime(year, month, 1).ToString("MMMM yyyy", CultureInfo.InvariantCulture);
            var rows = detail.Tickets.Select(t => new[]
            {
                $"#{t.TicketId:0000}",
                t.CustomerName,
                t.DeviceName,
                t.Amount.ToString("₱#,##0.00"),
                t.Date?.ToString("MMM d, yyyy") ?? "-",
                t.Status
            }).ToArray();

            using var dialog = new KpiDetailDialog(
                monthLabel,
                $"{detail.Tickets.Count} resolved ticket{(detail.Tickets.Count == 1 ? "" : "s")} — {detail.Revenue:₱#,##0.00} total",
                new[] { "Ticket", "Customer", "Device", "Amount", "Closed", "Outcome" },
                rows);
            dialog.ShowDialog(FindForm());
        }

        private async Task LoadDataAsync()
        {
            var salesTask = _controller.GetMonthlySalesAsync();
            var customersTask = _controller.GetTopCustomersAsync();
            var devicesTask = _controller.GetCommonValuesAsync("devices");
            var concernsTask = _controller.GetCommonValuesAsync("concerns");
            var feedbackTask = _controller.GetAllFeedbackAsync();
            var promoComparisonTask = _controller.GetPromotionComparisonAsync();
            var promoEffectivenessTask = _controller.GetPromotionEffectivenessAsync();
            await Task.WhenAll(salesTask, customersTask, devicesTask, concernsTask, feedbackTask, promoComparisonTask, promoEffectivenessTask);

            var sales = salesTask.Result;
            _salesData = sales;
            bool anySales = sales.Any(s => s.TicketCount > 0);
            _salesGrid.Visible = anySales;
            _salesEmptyLabel.Visible = !anySales;
            _salesPager.SetItems(sales);

            _customersData = customersTask.Result;
            _customersPager.SetItems(_customersData);

            var devices = devicesTask.Result;
            _devicesData = devices;
            _devicesMaxCount = devices.Count > 0 ? devices.Max(d => d.Count) : 1;
            _devicesGrid.Visible = devices.Count > 0;
            _devicesEmptyLabel.Visible = devices.Count == 0;
            _devicesPager.SetItems(devices);

            var concerns = concernsTask.Result;
            _concernsData = concerns;
            _concernsMaxCount = concerns.Count > 0 ? concerns.Max(c => c.Count) : 1;
            _concernsGrid.Visible = concerns.Count > 0;
            _concernsEmptyLabel.Visible = concerns.Count == 0;
            _concernsPager.SetItems(concerns);

            var feedback = feedbackTask.Result;
            _feedbackData = feedback;
            _feedbackGrid.Visible = feedback.Count > 0;
            _feedbackEmptyLabel.Visible = feedback.Count == 0;
            _feedbackPager.SetItems(feedback);

            var comparison = promoComparisonTask.Result;
            _promotionComparisonData = comparison;
            if (comparison != null)
                ApplyPromotionComparison(comparison);

            var effectiveness = promoEffectivenessTask.Result;
            _promotionsData = effectiveness;
            _promotionsGrid.Visible = effectiveness.Count > 0;
            _promotionsEmptyLabel.Visible = effectiveness.Count == 0;
            _promotionsPager.SetItems(effectiveness);
        }

        private void ApplyPromotionComparison(PromotionComparisonResponseDto comparison)
        {
            var with = comparison.WithPromotion;
            var without = comparison.WithoutPromotion;

            _withPromoCard.BigValue.Text = with.AvgTicketValue.ToString("₱#,##0.00");
            _withPromoCard.TicketsValue.Text = with.TicketCount.ToString();
            _withPromoCard.RevenueValue.Text = with.TotalRevenue.ToString("₱#,##0.00");
            _withPromoCard.DiscountValue.Text = with.TotalDiscount > 0 ? $"−{with.TotalDiscount:₱#,##0.00}" : "₱0.00";
            _withPromoCard.DiscountValue.ForeColor = with.TotalDiscount > 0 ? Theme.StatusUnrepairableText : Theme.TextPrimary;

            _withoutPromoCard.BigValue.Text = without.AvgTicketValue.ToString("₱#,##0.00");
            _withoutPromoCard.TicketsValue.Text = without.TicketCount.ToString();
            _withoutPromoCard.RevenueValue.Text = without.TotalRevenue.ToString("₱#,##0.00");
            _withoutPromoCard.DiscountValue.Text = "₱0.00";

            if (with.TicketCount == 0 || without.TicketCount == 0 || without.AvgTicketValue == 0)
            {
                _promoDeltaBadge.Text = "—";
                _promoDeltaBadge.BackColor = Theme.HeaderBg;
                _promoDeltaBadge.ForeColor = Theme.TextSecondary;
                _promoDeltaLabel.Text = "Not enough data yet to compare";
                return;
            }

            decimal delta = (with.AvgTicketValue - without.AvgTicketValue) / without.AvgTicketValue * 100;
            bool positive = delta >= 0;
            _promoDeltaBadge.Text = $"{(positive ? "+" : "")}{delta:0}%";
            _promoDeltaBadge.BackColor = positive ? Theme.StatusRepairedBg : Theme.StatusUnrepairableBg;
            _promoDeltaBadge.ForeColor = positive ? Theme.StatusRepairedText : Theme.StatusUnrepairableText;
            _promoDeltaLabel.Text = positive
                ? "higher avg. ticket value with a promotion"
                : "lower avg. ticket value with a promotion";
        }

        // ----------------------------------------------------------
        // PDF export
        // ----------------------------------------------------------

        /// <summary>Who to credit as the exporter on a PDF — the logged-in Admin.</summary>
        private string GeneratedByLabel() => $"{_controller.AdminName} (Admin)";

        private void ExportSalesReport()
        {
            var rows = _salesData.Select(s => new[]
            {
                new DateTime(s.Year, s.Month, 1).ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                s.TicketCount.ToString(),
                s.Revenue.ToString("₱#,##0.00")
            }).ToList();

            var chart = new PdfBarChart("Revenue by Month",
                _salesData.Select(s => (new DateTime(s.Year, s.Month, 1).ToString("MMM yyyy", CultureInfo.InvariantCulture), (double)s.Revenue)).ToList());

            SaveAndExportTable("Monthly Sales", "Last 12 months of resolved-ticket revenue",
                new[] { "Month", "Tickets Resolved", "Revenue" }, rows, "Monthly-Sales",
                generatedBy: GeneratedByLabel(), chart: chart);
        }

        private void ExportCustomersReport()
        {
            var rows = _customersData.Select(c => new[]
            {
                c.FullName, c.VisitCount.ToString(), c.TotalSpent.ToString("₱#,##0.00"), c.LastVisit.ToString("MMM d, yyyy")
            }).ToList();

            var chart = new PdfBarChart("Total Spent — Top Customers",
                _customersData.Take(8).Select(c => (c.FullName, (double)c.TotalSpent)).ToList());

            SaveAndExportTable("Top Customers", "Ranked by total spend",
                new[] { "Customer", "Visits", "Total Spent", "Last Visit" }, rows, "Top-Customers",
                generatedBy: GeneratedByLabel(), chart: chart);
        }

        private void ExportCommonRepairsReport()
        {
            var deviceRows = _devicesData.Select(d => new[] { d.Value, d.Count.ToString() }).ToList();
            var concernRows = _concernsData.Select(c => new[] { c.Value, c.Count.ToString() }).ToList();

            SaveAndExportReport("Most Common Repairs", "Which device types and stated concerns show up most often",
                Array.Empty<PdfSummaryStat>(),
                new[]
                {
                    new PdfReportSection("Most Common Devices", new[] { "Device", "Tickets" }, deviceRows),
                    new PdfReportSection("Most Common Repair Concerns", new[] { "Concern", "Tickets" }, concernRows)
                },
                "Common-Repairs",
                generatedBy: GeneratedByLabel(),
                charts: new[]
                {
                    new PdfBarChart("Most Common Devices", _devicesData.Select(d => (d.Value, (double)d.Count)).ToList()),
                    new PdfBarChart("Most Common Repair Concerns", _concernsData.Select(c => (c.Value, (double)c.Count)).ToList())
                });
        }

        private void ExportFeedbackReport()
        {
            var rows = _feedbackData.Select(f => new[]
            {
                f.CustomerName, f.DeviceName, string.IsNullOrWhiteSpace(f.HandledBy) ? "-" : f.HandledBy,
                f.TicketCreatedAt.ToString("MMM d, yyyy"), $"{f.Rating} / 5", f.Message, f.CreatedAt.ToString("MMM d, yyyy")
            }).ToList();

            var chart = new PdfBarChart("Feedback by Rating",
                Enumerable.Range(1, 5).Reverse()
                    .Select(r => ($"{r} star{(r == 1 ? "" : "s")}", (double)_feedbackData.Count(f => f.Rating == r)))
                    .ToList());

            SaveAndExportTable("Customer Feedback", "What customers said when they picked up their device",
                new[] { "Customer", "Device", "Handled By", "Created", "Rating", "Comment", "Date" }, rows, "Customer-Feedback",
                generatedBy: GeneratedByLabel(), chart: chart);
        }

        private void ExportPromotionsReport()
        {
            var rows = _promotionsData.Select(p => new[]
            {
                p.Title, p.Type, p.TimesUsed.ToString(), $"−{p.DiscountGiven:₱#,##0.00}",
                p.RevenueGenerated.ToString("₱#,##0.00"), p.AvgTicketValue.ToString("₱#,##0.00")
            }).ToList();

            var summary = new List<PdfSummaryStat>();
            var comparison = _promotionComparisonData;
            if (comparison != null && comparison.WithPromotion.TicketCount > 0)
            {
                summary.Add(new PdfSummaryStat("Avg. Ticket — With Promotion", comparison.WithPromotion.AvgTicketValue.ToString("₱#,##0.00")));
                summary.Add(new PdfSummaryStat("Avg. Ticket — No Promotion", comparison.WithoutPromotion.AvgTicketValue.ToString("₱#,##0.00")));
                summary.Add(new PdfSummaryStat("Total Discount Given", comparison.WithPromotion.TotalDiscount.ToString("₱#,##0.00")));
            }

            var chart = new PdfBarChart("Revenue Generated by Promotion",
                _promotionsData.Select(p => (p.Title, (double)p.RevenueGenerated)).ToList());

            SaveAndExportReport("Promotion Effectiveness", "Approved promotions' real usage/discount/revenue track record",
                summary,
                new[] { new PdfReportSection("Promotion Effectiveness", new[] { "Promotion", "Type", "Times Used", "Discount Given", "Revenue Generated", "Avg. Ticket Value" }, rows) },
                "Promotion-Effectiveness",
                generatedBy: GeneratedByLabel(), charts: new[] { chart });
        }

        /// <summary>Admin's single "everything in one PDF" export — a Summary of
        /// headline figures across the whole tenant, followed by every report
        /// section's full table, in the same order the sidebar lists them.</summary>
        private void ExportFullReport()
        {
            var summary = new List<PdfSummaryStat>
            {
                new("Revenue (12mo)", _salesData.Sum(s => s.Revenue).ToString("₱#,##0.00")),
                new("Top Customer", _customersData.OrderByDescending(c => c.TotalSpent).FirstOrDefault()?.FullName ?? "—"),
                new("Average Rating", _feedbackData.Count > 0 ? $"{_feedbackData.Average(f => f.Rating):0.0} ★" : "—"),
                new("Most Common Device", _devicesData.OrderByDescending(d => d.Count).FirstOrDefault()?.Value ?? "—"),
            };

            var sections = new List<PdfReportSection>
            {
                new("Monthly Sales", new[] { "Month", "Tickets Resolved", "Revenue" },
                    _salesData.Select(s => new[]
                    {
                        new DateTime(s.Year, s.Month, 1).ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                        s.TicketCount.ToString(), s.Revenue.ToString("₱#,##0.00")
                    }).ToList()),

                new("Top Customers", new[] { "Customer", "Visits", "Total Spent", "Last Visit" },
                    _customersData.Select(c => new[]
                    {
                        c.FullName, c.VisitCount.ToString(), c.TotalSpent.ToString("₱#,##0.00"), c.LastVisit.ToString("MMM d, yyyy")
                    }).ToList()),

                new("Most Common Devices", new[] { "Device", "Tickets" },
                    _devicesData.Select(d => new[] { d.Value, d.Count.ToString() }).ToList()),

                new("Most Common Repair Concerns", new[] { "Concern", "Tickets" },
                    _concernsData.Select(c => new[] { c.Value, c.Count.ToString() }).ToList()),

                new("Customer Feedback", new[] { "Customer", "Device", "Handled By", "Created", "Rating", "Comment", "Date" },
                    _feedbackData.Select(f => new[]
                    {
                        f.CustomerName, f.DeviceName, string.IsNullOrWhiteSpace(f.HandledBy) ? "-" : f.HandledBy,
                        f.TicketCreatedAt.ToString("MMM d, yyyy"), $"{f.Rating} / 5", f.Message, f.CreatedAt.ToString("MMM d, yyyy")
                    }).ToList()),

                new("Promotion Effectiveness", new[] { "Promotion", "Type", "Times Used", "Discount Given", "Revenue Generated", "Avg. Ticket Value" },
                    _promotionsData.Select(p => new[]
                    {
                        p.Title, p.Type, p.TimesUsed.ToString(), $"−{p.DiscountGiven:₱#,##0.00}",
                        p.RevenueGenerated.ToString("₱#,##0.00"), p.AvgTicketValue.ToString("₱#,##0.00")
                    }).ToList()),
            };

            using var dialog = new SaveFileDialog
            {
                Title = "Save Full Report PDF",
                Filter = "PDF Document|*.pdf",
                FileName = $"CRMS-Full-Report-{DateTime.Now:yyyy-MM-dd}.pdf"
            };
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

            var charts = new List<PdfBarChart>
            {
                new("Revenue by Month", _salesData.Select(s => (new DateTime(s.Year, s.Month, 1).ToString("MMM yyyy", CultureInfo.InvariantCulture), (double)s.Revenue)).ToList())
            };

            try
            {
                PdfExportService.ExportReport("Full Business Report", "Every report section, as of today", summary, sections, dialog.FileName,
                    generatedBy: GeneratedByLabel(), charts: charts);
                MessageBox.Show(FindForm(), "PDF saved successfully.", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), $"Could not save the PDF:\n{ex.Message}", "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveAndExportTable(string title, string subtitle, string[] columns, IReadOnlyList<string[]> rows, string suggestedFileName,
            string generatedBy = "", PdfBarChart? chart = null)
        {
            using var dialog = new SaveFileDialog
            {
                Title = $"Save {title} PDF",
                Filter = "PDF Document|*.pdf",
                FileName = $"{suggestedFileName}-{DateTime.Now:yyyy-MM-dd}.pdf"
            };
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

            try
            {
                PdfExportService.ExportTable(title, subtitle, columns, rows, dialog.FileName, generatedBy: generatedBy, chart: chart);
                MessageBox.Show(FindForm(), "PDF saved successfully.", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), $"Could not save the PDF:\n{ex.Message}", "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveAndExportReport(string title, string subtitle, IReadOnlyList<PdfSummaryStat> summary, IReadOnlyList<PdfReportSection> sections, string suggestedFileName,
            string generatedBy = "", IReadOnlyList<PdfBarChart>? charts = null)
        {
            using var dialog = new SaveFileDialog
            {
                Title = $"Save {title} PDF",
                Filter = "PDF Document|*.pdf",
                FileName = $"{suggestedFileName}-{DateTime.Now:yyyy-MM-dd}.pdf"
            };
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

            try
            {
                PdfExportService.ExportReport(title, subtitle, summary, sections, dialog.FileName, generatedBy: generatedBy, charts: charts);
                MessageBox.Show(FindForm(), "PDF saved successfully.", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), $"Could not save the PDF:\n{ex.Message}", "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
