using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS.winforms; // reuses RoundedPanel/RoundedButton/RoundedTextBox from Login.cs
using CRMS.winforms.Api;
using CRMS.winforms.Controllers;
using CRMS.winforms.Shared;

namespace CRMS.winforms.StaffView
{
    /// <summary>
    /// Popup shown when Staff clicks "+ Add Customer": collects the walk-in
    /// customer's details, the device concern, an estimated service fee built
    /// from the parts/services picked, and requires the 24-hour-no-cancellation
    /// terms to be accepted before the ticket can be submitted.
    ///
    /// LAYOUT NOTE (this was the bug): WinForms lays out docked children in
    /// REVERSE order of the Controls collection — the control added LAST is
    /// docked FIRST and takes the outermost edge. So the Dock=Fill scroll area
    /// must be added FIRST, then the Bottom footer LAST. Terms & Agreement is
    /// just the last card inside the scroll area now (not its own Dock=Bottom
    /// bar), so the footer is the only other docked piece to worry about.
    /// </summary>
    public class AddCustomerDialog : Form
    {
        // ----- Colors (matches the original walk-in intake design) -----
        private static readonly Color CardColor = Color.White;
        private static readonly Color CardBorder = Color.FromArgb(232, 234, 238);
        private static readonly Color TitleColor = Color.FromArgb(33, 37, 41);
        private static readonly Color SubtleGray = Color.FromArgb(140, 144, 153);
        private static readonly Color Accent = Color.FromArgb(58, 150, 150);
        private static readonly Color AccentHover = Color.FromArgb(47, 130, 130);
        private static readonly Color AccentSoftBg = Color.FromArgb(230, 244, 244);
        private static readonly Color FieldFill = Color.FromArgb(249, 249, 251);
        private static readonly Color DisabledGray = Color.FromArgb(163, 169, 181);
        private static readonly Color DisabledGrayHover = Color.FromArgb(150, 156, 168);
        private static readonly Color PageBg = Color.FromArgb(237, 239, 242);

        // ----- Font sizes (bumped up across the whole dialog for readability) -----
        private const float TitleFontSize = 16F;
        private const float SubtitleFontSize = 10.5F;
        private const float SectionTitleFontSize = 11.5F;
        private const float IconFontSize = 13F;
        private const float CaptionFontSize = 9.5F;
        private const float InputFontSize = 11.5F;
        private const float ChipFontSize = 10F;
        private const float TotalCaptionFontSize = 9.5F;
        private const float TotalCountFontSize = 10F;
        private const float TotalValueFontSize = 16F;
        private const float TermsFontSize = 10.5F;
        private const float CheckboxFontSize = 11F;
        private const float ErrorFontSize = 10F;
        private const float ButtonFontSize = 11.5F;

        // ----- Layout constants (grown to fit the bigger text) -----
        private const int HeaderHeight = 92;
        private const int CardGap = 12;
        private const int CardPad = 24;
        private const int SectionHeaderHeight = 68;   // space reserved for the icon+title row
        private const int FieldBoxHeight = 42;         // was 36
        private const int FieldBlockHeight = 74;       // was 62 — caption + box + gap to next row
        private const int FieldCaptionGap = 22;        // was 18 — caption -> box vertical offset

        private static readonly (string Name, decimal Price)[] ServiceOptions =
        {
            ("Diagnostic Fee", 300.00m),
            ("RAM Module", 1500.00m),
            ("SSD (256 GB)", 2500.00m),
            ("Battery Replacement", 1800.00m),
            ("Screen / Display", 4500.00m),
            ("Thermal Paste", 200.00m),
            ("Keyboard Replacement", 1200.00m),
            ("Charger / Adapter", 850.00m),
        };

        private RoundedTextBox txtFirstName = null!;
        private RoundedTextBox txtMiddleName = null!;
        private RoundedTextBox txtLastName = null!;
        private RoundedTextBox txtPhone = null!;
        private RoundedTextBox txtEmail = null!;
        private RoundedTextBox txtDeviceName = null!;
        private RoundedMultilineBox txtConcern = null!;
        private Label lblTotalValue = null!;
        private Label lblSelectedCount = null!;
        private CheckBox chkAcceptTerms = null!;
        private RoundedButton btnSubmit = null!;
        private Label lblError = null!;

        private Panel _scroll = null!;
        private RoundedPanel[] _cards = Array.Empty<RoundedPanel>();
        private RoundedPanel _serviceCard = null!;
        private FlowLayoutPanel _chipFlow = null!;
        private RoundedPanel _totalStrip = null!;
        private RoundedPanel _termsCard = null!;

        private RoundedPanel _customerCard = null!;
        private readonly List<Control> _customerRow0 = new(); // Name captions + boxes
        private readonly List<Control> _customerRow1 = new(); // Phone/Email captions + boxes
        private int _searchResultsTop;      // y where the results list / linked badge sit, right under the search box
        private int _customerFieldsTopIdle; // y the Name row sits at when nothing is being searched
        private const int SearchResultsReserve = 88; // extra height reserved only while results/linked badge are actually shown
        private Label _termsText = null!;

        private readonly HashSet<string> _selectedServices = new();
        private readonly Func<string, Task<List<CustomerSearchResultDto>>>? _searchCustomers;
        private readonly Func<Task<List<PromotionDto>>>? _getApprovedPromotions;
        private readonly Func<Task<string?>>? _getCurrentTerms;
        private ComboBox cboPromotion = null!;
        private Label lblPromotionCaption = null!;
        private Label lblDiscountCaption = null!;
        private RoundedTextBox txtDiscount = null!;
        private Label lblSavingsNote = null!;

        /// <summary>Null when no promotion is applied (or the picker is disabled).</summary>
        public int? PromotionIdValue => (cboPromotion?.SelectedItem as PromotionOption?)?.PromotionId;

        private decimal SubtotalValue => _selectedServices.Sum(name => ServiceOptions.First(o => o.Name == name).Price);

        /// <summary>How much of the subtotal a promotion knocks off — only
        /// applies when a promotion is actually selected, so leftover text in a
        /// hidden discount box can never silently discount a plain ticket.</summary>
        private decimal DiscountValue
        {
            get
            {
                if (PromotionIdValue is null || txtDiscount is null) return 0m;
                return decimal.TryParse(txtDiscount.Value.Trim(), out var amount) ? Math.Max(0, amount) : 0m;
            }
        }

        /// <summary>The price before the promotion's discount — only set (sent
        /// to the API) when a real discount was actually entered, so a ticket
        /// with a promotion picked but ₱0 discount still records nothing.</summary>
        public decimal? OriginalPriceValue => (PromotionIdValue.HasValue && DiscountValue > 0) ? SubtotalValue : (decimal?)null;

        private RoundedTextBox txtSearch = null!;
        private ListBox lstSearchResults = null!;
        private Label lblLinkedBadge = null!;
        private LinkLabel lnkClearLink = null!;

        /// <summary>Set once a returning customer is picked from search results —
        /// when non-null, the API links the ticket to this customer instead of
        /// creating a new one (name/phone/email fields are then ignored server-side).</summary>
        public int? ExistingCustomerIdValue { get; private set; }

        public string FirstNameValue => txtFirstName.Value.Trim();
        public string MiddleNameValue => txtMiddleName.Value.Trim();
        public string LastNameValue => txtLastName.Value.Trim();
        public string PhoneValue => txtPhone.Value.Trim();
        public string EmailValue => txtEmail.Value.Trim();
        public string DeviceNameValue => txtDeviceName.Value.Trim();
        public string ConcernValue => txtConcern.Value.Trim();
        public decimal EstimatedCostValue => Math.Max(0, SubtotalValue - DiscountValue);
        public IReadOnlyList<TicketPart> SelectedPartsValue =>
            _selectedServices.Select(name => new TicketPart(name, ServiceOptions.First(o => o.Name == name).Price)).ToList();

        /// <summary>searchCustomers: looks up returning customers by name/phone/email
        /// within the current tenant. Pass null to disable the search step entirely
        /// (dialog behaves exactly as before — always creates a new customer).
        /// getApprovedPromotions: loads the tenant's Approved promotions for the
        /// "Apply Promotion" picker. Pass null to hide that picker entirely.
        /// getCurrentTerms: fetches this tenant's currently published Terms &amp;
        /// Conditions text. Pass null, or have it return null/empty, to keep the
        /// built-in default paragraph below instead.</summary>
        public AddCustomerDialog(
            Func<string, Task<List<CustomerSearchResultDto>>>? searchCustomers = null,
            Func<Task<List<PromotionDto>>>? getApprovedPromotions = null,
            Func<Task<string?>>? getCurrentTerms = null)
        {
            _searchCustomers = searchCustomers;
            _getApprovedPromotions = getApprovedPromotions;
            _getCurrentTerms = getCurrentTerms;
            BuildLayout();
            if (_getApprovedPromotions != null)
                Load += async (s, e) => await LoadApprovedPromotionsAsync();
            if (_getCurrentTerms != null)
                Load += async (s, e) => await LoadCurrentTermsAsync();
        }

        /// <summary>Replaces the default terms paragraph with this tenant's
        /// currently published version, if Admin has published one — silently
        /// keeps the default otherwise (no published version, or the request failed).</summary>
        private async Task LoadCurrentTermsAsync()
        {
            string? content = await _getCurrentTerms!();
            if (string.IsNullOrWhiteSpace(content)) return;

            _termsText.Text = content;
            ReflowCards();
        }

        private readonly record struct PromotionOption(int? PromotionId, string Display)
        {
            public override string ToString() => Display;
        }

        private void BuildLayout()
        {
            Text = "Add New Customer";
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(720, 780);
            MinimumSize = new Size(660, 600);
            WindowState = FormWindowState.Maximized;
            BackColor = PageBg;
            Font = new Font("Segoe UI", 9F);

            var scroll = BuildScrollArea();   // Dock = Fill (Customer/Device/Service/Terms cards)
            var footer = BuildFooter();       // Dock = Bottom
            var header = BuildHeader();       // Dock = Top

            // Add order is load-bearing: Fill first, footer next, header last.
            Controls.Add(scroll);
            Controls.Add(footer);
            Controls.Add(header);

            ReflowCards();
        }

        // ===================== Header =====================
        private Panel BuildHeader()
        {
            var header = new Panel { Dock = DockStyle.Top, Height = HeaderHeight, BackColor = CardColor };

            var title = new Label
            {
                Text = "Add New Customer",
                Font = new Font("Segoe UI", TitleFontSize, FontStyle.Bold),
                ForeColor = TitleColor,
                AutoSize = true,
                Location = new Point(CardPad, 18)
            };
            header.Controls.Add(title);

            header.Controls.Add(new Label
            {
                Text = "Fill in all sections to generate a ticket",
                Font = new Font("Segoe UI", SubtitleFontSize),
                ForeColor = SubtleGray,
                AutoSize = true,
                // sits below the measured title instead of a hard-coded y that overlapped it
                Location = new Point(CardPad, 18 + title.PreferredHeight + 6)
            });

            header.Paint += (s, e) =>
            {
                using var pen = new Pen(CardBorder, 1);
                e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
            };
            return header;
        }

        // ===================== Footer =====================
        private Panel BuildFooter()
        {
            const int footerHeight = 80;
            const int footerPad = 16;
            const int btnHeight = 46;
            const int btnGap = 12;
            const int cancelWidth = 120;

            var footer = new Panel { Dock = DockStyle.Bottom, Height = footerHeight, BackColor = CardColor };
            footer.Paint += (s, e) =>
            {
                using var pen = new Pen(CardBorder, 1);
                e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
            };

            int btnY = (footerHeight - btnHeight) / 2;

            var btnCancel = new RoundedButton
            {
                Text = "Cancel",
                Location = new Point(footerPad, btnY),
                Size = new Size(cancelWidth, btnHeight),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                CornerRadius = 0,
                BackColor = Color.White,
                HoverColor = Color.FromArgb(245, 246, 248),
                ForeColor = TitleColor,
                Font = new Font("Segoe UI", ButtonFontSize, FontStyle.Bold)
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            footer.Controls.Add(btnCancel);

            btnSubmit = new RoundedButton
            {
                Text = "Confirm & Generate Ticket",
                Location = new Point(footerPad + cancelWidth + btnGap, btnY),
                Size = new Size(300, btnHeight),   // real width comes from the anchor on first layout
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                CornerRadius = 0,
                BackColor = DisabledGray,
                HoverColor = DisabledGrayHover,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", ButtonFontSize, FontStyle.Bold),
                Enabled = false
            };
            btnSubmit.Click += BtnSubmit_Click;
            footer.Controls.Add(btnSubmit);

            // Anchor math needs a correct starting width, so set it once the bar has one.
            footer.Resize += (s, e) =>
                btnSubmit.Width = Math.Max(160, footer.ClientSize.Width - footerPad * 2 - cancelWidth - btnGap);

            return footer;
        }

        // ===================== Scrollable content =====================
        private Panel BuildScrollArea()
        {
            _scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = PageBg,
                Padding = new Padding(16, 12, 4, 24)
            };

            int cardWidth = 660;
            _cards = new[]
            {
                BuildCustomerCard(cardWidth),
                BuildDeviceCard(cardWidth),
                BuildServiceCard(cardWidth),
                BuildTermsCard(cardWidth)
            };
            foreach (var card in _cards) _scroll.Controls.Add(card);

            _scroll.Resize += (s, e) => ReflowCards();
            return _scroll;
        }

        /// <summary>Measures the panel's real usable width (scrollbar included) and
        /// restacks the cards vertically. Cheaper and more reliable than Anchor on
        /// an AutoScroll parent.</summary>
        private void ReflowCards()
        {
            if (_cards.Length == 0) return;

            int available = _scroll.ClientSize.Width - _scroll.Padding.Horizontal;
            if (available <= 0) return;

            _scroll.SuspendLayout();
            int y = _scroll.Padding.Top;
            foreach (var card in _cards)
            {
                card.Width = available;
                if (card == _termsCard) SyncTermsCardHeight();
                card.Location = new Point(_scroll.Padding.Left, y);
                y += card.Height + CardGap;
            }
            _scroll.ResumeLayout();
        }

        private RoundedPanel NewCard(int width)
        {
            return new RoundedPanel { Width = width, BackColor = CardColor, CornerRadius = 16, BorderColor = CardBorder };
        }

        private void AddSectionHeader(RoundedPanel card, string icon, string title)
        {
            var iconBadge = new Panel { Size = new Size(36, 36), Location = new Point(20, 18), BackColor = AccentSoftBg };
            MakeRound(iconBadge);
            card.Controls.Add(iconBadge);
            iconBadge.Controls.Add(new Label
            {
                Text = icon,
                Font = new Font("Segoe UI Emoji", IconFontSize),
                Size = new Size(36, 36),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            });

            var titleLbl = new Label
            {
                Text = title,
                UseMnemonic = false,
                Font = new Font("Segoe UI", SectionTitleFontSize, FontStyle.Bold),
                ForeColor = TitleColor,
                AutoSize = true
            };
            card.Controls.Add(titleLbl);
            titleLbl.Location = new Point(66, 18 + (36 - titleLbl.PreferredHeight) / 2);
        }

        private void AddLabeledField(RoundedPanel card, out RoundedTextBox box, string caption, string placeholder,
            int x, int y, int width, bool stretchWithCard = false, List<Control>? trackIn = null, TextInputMode inputMode = TextInputMode.Any)
        {
            var label = new Label
            {
                Text = caption,
                Font = new Font("Segoe UI", CaptionFontSize, FontStyle.Bold),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(x, y)
            };
            card.Controls.Add(label);
            box = new RoundedTextBox
            {
                Location = new Point(x, y + FieldCaptionGap),
                Size = new Size(width, FieldBoxHeight),
                Placeholder = placeholder,
                TextSize = InputFontSize,
                InputMode = inputMode
            };
            if (stretchWithCard)
                box.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            card.Controls.Add(box);

            if (trackIn != null)
            {
                trackIn.Add(label);
                trackIn.Add(box);
            }
        }

        // ----- Customer Information -----
        private RoundedPanel BuildCustomerCard(int width)
        {
            var card = NewCard(width);
            _customerCard = card;
            AddSectionHeader(card, "\U0001F464", "Customer Information");

            int fieldsTop = SectionHeaderHeight;
            if (_searchCustomers != null)
                fieldsTop = BuildReturningCustomerSearch(card, width, fieldsTop);
            _customerFieldsTopIdle = fieldsTop;

            int gap = 16;
            int triWidth = (width - CardPad * 2 - gap * 2) / 3;
            int lastTriWidth = (width - CardPad * 2) - triWidth * 2 - gap * 2;
            int halfWidth = (width - CardPad * 2 - gap) / 2;

            AddLabeledField(card, out txtFirstName, "FIRST NAME", "e.g. Maria", CardPad, fieldsTop, triWidth, trackIn: _customerRow0, inputMode: TextInputMode.Letters);
            AddLabeledField(card, out txtMiddleName, "MIDDLE NAME", "e.g. Anne", CardPad + triWidth + gap, fieldsTop, triWidth, trackIn: _customerRow0, inputMode: TextInputMode.Letters);
            AddLabeledField(card, out txtLastName, "LAST NAME", "e.g. Santos", CardPad + (triWidth + gap) * 2, fieldsTop, lastTriWidth, stretchWithCard: true, trackIn: _customerRow0, inputMode: TextInputMode.Letters);

            AddLabeledField(card, out txtPhone, "PHONE NUMBER", "+1 (555) 000-0000", CardPad, fieldsTop + FieldBlockHeight, halfWidth, trackIn: _customerRow1, inputMode: TextInputMode.Phone);
            AddLabeledField(card, out txtEmail, "EMAIL ADDRESS", "customer@email.com", CardPad + halfWidth + gap, fieldsTop + FieldBlockHeight, halfWidth, stretchWithCard: true, trackIn: _customerRow1);

            RepositionCustomerFields();
            return card;
        }

        /// <summary>Moves the Name/Phone/Email fields (and resizes the card) to sit
        /// right under the search box when idle, or make room for the results list
        /// / linked-customer badge only while one of those is actually visible —
        /// instead of always reserving that space whether or not anything's there.</summary>
        private void RepositionCustomerFields()
        {
            int fieldsTop = _customerFieldsTopIdle;
            if (lstSearchResults.Visible || lblLinkedBadge.Visible)
                fieldsTop += SearchResultsReserve;

            foreach (var c in _customerRow0)
                c.Top = c is Label ? fieldsTop : fieldsTop + FieldCaptionGap;
            foreach (var c in _customerRow1)
                c.Top = c is Label ? fieldsTop + FieldBlockHeight : fieldsTop + FieldBlockHeight + FieldCaptionGap;

            _customerCard.Height = fieldsTop + FieldBlockHeight + FieldCaptionGap + FieldBoxHeight + 20;
            ReflowCards();
        }

        /// <summary>Adds a search box + results list above the name/phone/email
        /// fields, so Staff can attach the ticket to a returning customer instead
        /// of always creating a new one. Returns the y-offset the name fields
        /// should now start at.</summary>
        private int BuildReturningCustomerSearch(RoundedPanel card, int width, int top)
        {
            card.Controls.Add(new Label
            {
                Text = "RETURNING CUSTOMER? SEARCH BY NAME OR PHONE",
                Font = new Font("Segoe UI", CaptionFontSize, FontStyle.Bold),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(CardPad, top)
            });

            txtSearch = new RoundedTextBox
            {
                Location = new Point(CardPad, top + FieldCaptionGap),
                Size = new Size(width - CardPad * 2, FieldBoxHeight),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Placeholder = "Start typing a name or phone number...",
                TextSize = InputFontSize
            };
            card.Controls.Add(txtSearch);

            _searchResultsTop = top + FieldCaptionGap + FieldBoxHeight + 6;

            lstSearchResults = new ListBox
            {
                Location = new Point(CardPad, _searchResultsTop),
                Size = new Size(width - CardPad * 2, SearchResultsReserve),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", InputFontSize - 1),
                Visible = false
            };
            lstSearchResults.Click += LstSearchResults_Click;
            card.Controls.Add(lstSearchResults);

            lblLinkedBadge = new Label
            {
                Font = new Font("Segoe UI", CaptionFontSize, FontStyle.Bold),
                ForeColor = Accent,
                AutoSize = true,
                Location = new Point(CardPad, _searchResultsTop),
                Visible = false
            };
            card.Controls.Add(lblLinkedBadge);

            lnkClearLink = new LinkLabel
            {
                Text = "Not this customer — search again",
                Font = new Font("Segoe UI", CaptionFontSize),
                AutoSize = true,
                Visible = false
            };
            lnkClearLink.Click += (s, e) => ClearSelectedCustomer();
            card.Controls.Add(lnkClearLink);

            var searchTimer = new System.Windows.Forms.Timer { Interval = 350 };
            searchTimer.Tick += async (s, e) =>
            {
                searchTimer.Stop();
                await RunSearchAsync(txtSearch.Value.Trim());
            };
            txtSearch.ValueChanged += (s, e) =>
            {
                if (ExistingCustomerIdValue.HasValue) return; // programmatic fill after picking a result
                searchTimer.Stop();
                searchTimer.Start();
            };

            // No reservation yet — RepositionCustomerFields() expands this only
            // once the results list or linked badge actually has something to show.
            return _searchResultsTop + 16;
        }

        private List<CustomerSearchResultDto> _lastSearchResults = new();

        private async Task RunSearchAsync(string query)
        {
            if (_searchCustomers is null || query.Length < 2)
            {
                lstSearchResults.Visible = false;
                RepositionCustomerFields();
                return;
            }

            _lastSearchResults = await _searchCustomers(query);
            lstSearchResults.Items.Clear();
            foreach (var c in _lastSearchResults)
                lstSearchResults.Items.Add($"{c.FullName} — {c.PhoneNumber}  ({c.TicketCount} past ticket{(c.TicketCount == 1 ? "" : "s")})");

            lstSearchResults.Visible = _lastSearchResults.Count > 0;
            RepositionCustomerFields();
        }

        private void LstSearchResults_Click(object? sender, EventArgs e)
        {
            int index = lstSearchResults.SelectedIndex;
            if (index < 0 || index >= _lastSearchResults.Count) return;

            var picked = _lastSearchResults[index];
            ExistingCustomerIdValue = picked.CustomerId;

            txtSearch.Visible = false;
            lstSearchResults.Visible = false;

            lblLinkedBadge.Text = $"✓ Linked to existing customer: {picked.FullName} — {picked.PhoneNumber}";
            lblLinkedBadge.Visible = true;
            lnkClearLink.Location = new Point(CardPad, lblLinkedBadge.Bottom + 4);
            lnkClearLink.Visible = true;
            RepositionCustomerFields();

            // Pre-fill for display only — ignored by the API once ExistingCustomerIdValue is set.
            var nameParts = picked.FullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            txtFirstName.Value = nameParts.Length > 0 ? nameParts[0] : picked.FullName;
            txtLastName.Value = nameParts.Length > 1 ? nameParts[1] : "";
            txtMiddleName.Value = "";
            txtPhone.Value = picked.PhoneNumber;
            txtEmail.Value = picked.Email;
        }

        private void ClearSelectedCustomer()
        {
            ExistingCustomerIdValue = null;
            txtSearch.Value = "";
            txtSearch.Visible = true;
            lblLinkedBadge.Visible = false;
            lnkClearLink.Visible = false;
            RepositionCustomerFields();

            txtFirstName.Value = "";
            txtMiddleName.Value = "";
            txtLastName.Value = "";
            txtPhone.Value = "";
            txtEmail.Value = "";
        }

        // ----- Device Information -----
        private RoundedPanel BuildDeviceCard(int width)
        {
            var card = NewCard(width);
            AddSectionHeader(card, "\U0001F4BB", "Device Information");

            int fieldsTop = SectionHeaderHeight;
            AddLabeledField(card, out txtDeviceName, "DEVICE NAME / MODEL", "e.g. MacBook Pro 14\" M3 (2023)",
                CardPad, fieldsTop, width - CardPad * 2, stretchWithCard: true);

            card.Controls.Add(new Label
            {
                Text = "CUSTOMER'S CONCERN",
                Font = new Font("Segoe UI", CaptionFontSize, FontStyle.Bold),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(CardPad, fieldsTop + FieldBlockHeight)
            });
            txtConcern = new RoundedMultilineBox
            {
                Location = new Point(CardPad, fieldsTop + FieldBlockHeight + FieldCaptionGap),
                Size = new Size(width - CardPad * 2, 84),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Placeholder = "Describe the issue in the customer's own words...",
                TextSize = InputFontSize
            };
            card.Controls.Add(txtConcern);

            card.Height = fieldsTop + FieldBlockHeight + FieldCaptionGap + 84 + 20;
            return card;
        }

        // ----- Repair Parts & Tools -----
        private RoundedPanel BuildServiceCard(int width)
        {
            _serviceCard = NewCard(width);
            AddSectionHeader(_serviceCard, "\U0001F527", "Repair Parts & Tools");

            // FlowLayoutPanel instead of hand-computed chip positions: the chips
            // re-wrap themselves when the dialog is maximized or resized, and the
            // panel reports its own height so the total strip can never be covered.
            _chipFlow = new FlowLayoutPanel
            {
                Location = new Point(CardPad, SectionHeaderHeight),
                Width = width - CardPad * 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };
            foreach (var option in ServiceOptions)
                _chipFlow.Controls.Add(MakeServiceChip(option.Name, option.Price));
            _serviceCard.Controls.Add(_chipFlow);

            _totalStrip = new RoundedPanel
            {
                Location = new Point(CardPad, _chipFlow.Bottom + 16),
                Size = new Size(width - CardPad * 2, 80),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = FieldFill,
                CornerRadius = 12
            };
            _totalStrip.Resize += (s, e) => PositionTotalValue(_totalStrip);
            _serviceCard.Controls.Add(_totalStrip);

            _totalStrip.Controls.Add(new Label
            {
                Text = "ESTIMATED TOTAL",
                Font = new Font("Segoe UI", TotalCaptionFontSize, FontStyle.Bold),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(16, 12)
            });
            lblSelectedCount = new Label
            {
                Text = "0 items selected",
                Font = new Font("Segoe UI", TotalCountFontSize),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(16, 34)
            };
            _totalStrip.Controls.Add(lblSelectedCount);

            lblTotalValue = new Label
            {
                Text = 0m.ToString("₱#,##0.00"),
                Font = new Font("Segoe UI", TotalValueFontSize, FontStyle.Bold),
                ForeColor = Accent,
                AutoSize = true
            };
            _totalStrip.Controls.Add(lblTotalValue);
            PositionTotalValue(_totalStrip);

            lblSavingsNote = new Label
            {
                Font = new Font("Segoe UI", TotalCountFontSize, FontStyle.Italic),
                ForeColor = Color.FromArgb(24, 130, 80),
                AutoSize = true,
                Visible = false
            };
            _totalStrip.Controls.Add(lblSavingsNote);

            if (_getApprovedPromotions != null)
            {
                lblPromotionCaption = new Label
                {
                    Text = "APPLY PROMOTION (OPTIONAL)",
                    Font = new Font("Segoe UI", CaptionFontSize, FontStyle.Bold),
                    ForeColor = SubtleGray,
                    AutoSize = true,
                    Location = new Point(CardPad, _totalStrip.Bottom + 16)
                };
                _serviceCard.Controls.Add(lblPromotionCaption);
                cboPromotion = new ComboBox
                {
                    Location = new Point(CardPad, _totalStrip.Bottom + 38),
                    Size = new Size(width - CardPad * 2, FieldBoxHeight),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    Font = new Font("Segoe UI", InputFontSize),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    FlatStyle = FlatStyle.Flat
                };
                cboPromotion.Items.Add(new PromotionOption(null, "No promotion applied"));
                cboPromotion.SelectedIndex = 0;
                _serviceCard.Controls.Add(cboPromotion);

                lblDiscountCaption = new Label
                {
                    Text = "DISCOUNT AMOUNT (₱)",
                    Font = new Font("Segoe UI", CaptionFontSize, FontStyle.Bold),
                    ForeColor = SubtleGray,
                    AutoSize = true,
                    Visible = false
                };
                _serviceCard.Controls.Add(lblDiscountCaption);

                txtDiscount = new RoundedTextBox
                {
                    Size = new Size(width - CardPad * 2, FieldBoxHeight),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    Placeholder = "0.00",
                    TextSize = InputFontSize,
                    Visible = false
                };
                txtDiscount.ValueChanged += (s, e) => RefreshTotalDisplay();
                _serviceCard.Controls.Add(txtDiscount);

                // Recording a discount only makes sense once a real promotion is
                // picked — hide the amount box otherwise, so it can't linger with
                // a stale value that would otherwise still get applied.
                cboPromotion.SelectedIndexChanged += (s, e) =>
                {
                    bool hasPromotion = PromotionIdValue.HasValue;
                    lblDiscountCaption.Visible = hasPromotion;
                    txtDiscount.Visible = hasPromotion;
                    if (!hasPromotion) txtDiscount.Value = "";
                    SyncServiceCardHeight();
                    RefreshTotalDisplay();
                };
            }

            // When the chips re-wrap to fewer/more rows, push the total strip and
            // grow the card, then restack the cards below it.
            _chipFlow.SizeChanged += (s, e) => SyncServiceCardHeight();
            SyncServiceCardHeight();

            return _serviceCard;
        }

        /// <summary>Keeps the total strip's numbers (and the small "you saved"
        /// note) in sync with the current service selection and discount entry —
        /// called on every input that could change either.</summary>
        private void RefreshTotalDisplay()
        {
            if (_totalStrip is null || lblTotalValue is null) return;

            lblTotalValue.Text = EstimatedCostValue.ToString("₱#,##0.00");

            if (lblSavingsNote is null)
            {
                PositionTotalValue(_totalStrip);
                return;
            }

            decimal discount = DiscountValue;
            lblSavingsNote.Visible = discount > 0;
            if (discount > 0)
                lblSavingsNote.Text = $"Subtotal {SubtotalValue:₱#,##0.00} · You saved {discount:₱#,##0.00}";

            PositionTotalValue(_totalStrip);
        }

        private async Task LoadApprovedPromotionsAsync()
        {
            if (_getApprovedPromotions is null || cboPromotion is null) return;

            var promotions = await _getApprovedPromotions();
            foreach (var promo in promotions)
                cboPromotion.Items.Add(new PromotionOption(promo.PromotionId, promo.Title));
        }

        private void SyncServiceCardHeight()
        {
            _totalStrip.Top = _chipFlow.Bottom + 16;

            int bottomContentY = _totalStrip.Bottom;
            if (cboPromotion != null)
            {
                lblPromotionCaption.Top = _totalStrip.Bottom + 16;
                cboPromotion.Top = _totalStrip.Bottom + 38;
                bottomContentY = cboPromotion.Bottom;

                if (txtDiscount.Visible)
                {
                    lblDiscountCaption.Top = cboPromotion.Bottom + 16;
                    lblDiscountCaption.Left = cboPromotion.Left;
                    txtDiscount.Top = cboPromotion.Bottom + 38;
                    txtDiscount.Left = cboPromotion.Left;
                    bottomContentY = txtDiscount.Bottom;
                }
            }

            _serviceCard.Height = bottomContentY + 20;
            ReflowCards();
        }

        private void PositionTotalValue(RoundedPanel totalStrip)
        {
            // A generous right margin (24, not 16) keeps right-aligned text
            // clear of the panel's own rounded bottom-right corner — text sitting
            // flush in that corner was getting visibly clipped by the curve.
            const int rightMargin = 24;
            bool showingSavings = lblSavingsNote is { Visible: true };

            if (showingSavings)
            {
                lblTotalValue.Location = new Point(totalStrip.Width - lblTotalValue.PreferredWidth - rightMargin, 14);
                lblSavingsNote.Location = new Point(totalStrip.Width - lblSavingsNote.PreferredWidth - rightMargin, lblTotalValue.Bottom + 4);
            }
            else
            {
                lblTotalValue.Location = new Point(totalStrip.Width - lblTotalValue.PreferredWidth - rightMargin, totalStrip.Height / 2 - lblTotalValue.PreferredHeight / 2);
            }
        }

        private RoundedButton MakeServiceChip(string name, decimal price)
        {
            var font = new Font("Segoe UI", ChipFontSize);
            var chip = new RoundedButton
            {
                Height = 58,
                Margin = new Padding(0, 0, 10, 10),
                BackColor = FieldFill,
                HoverColor = Color.FromArgb(240, 241, 245),
                ForeColor = TitleColor,
                Font = font,
                CornerRadius = 12,
                Text = $"{name}\n{price:₱#,##0.00}"
            };
            int textWidth = TextRenderer.MeasureText(name, chip.Font).Width;
            int priceWidth = TextRenderer.MeasureText(price.ToString("₱#,##0.00"), font).Width;
            chip.Width = Math.Max(textWidth, priceWidth) + 52; // +room for the ✓ prefix when selected
            chip.Click += (s, e) => ToggleService(name, chip);
            return chip;
        }

        private void ToggleService(string name, RoundedButton chip)
        {
            var option = ServiceOptions.First(o => o.Name == name);
            bool selected = !_selectedServices.Contains(name);

            if (selected) _selectedServices.Add(name);
            else _selectedServices.Remove(name);

            chip.BackColor = selected ? AccentSoftBg : FieldFill;
            chip.HoverColor = selected ? AccentSoftBg : Color.FromArgb(240, 241, 245);
            chip.ForeColor = selected ? Accent : TitleColor;
            chip.Font = new Font("Segoe UI", ChipFontSize, selected ? FontStyle.Bold : FontStyle.Regular);
            chip.Text = selected ? $"✓  {name}\n{option.Price:₱#,##0.00}" : $"{name}\n{option.Price:₱#,##0.00}";
            chip.Invalidate();

            lblSelectedCount.Text = $"{_selectedServices.Count} item{(_selectedServices.Count == 1 ? "" : "s")} selected";
            RefreshTotalDisplay();
        }

        // ----- Terms & Agreement (now the last card in the scrollable list,
        // same as Customer/Device/Service Information — no longer a separate
        // bar pinned above the footer). -----
        private RoundedPanel BuildTermsCard(int width)
        {
            _termsCard = NewCard(width);
            AddSectionHeader(_termsCard, "\U0001F4C4", "Terms & Agreement");

            _termsText = new Label
            {
                Text = "By submitting this service request, the customer agrees to the following terms: " +
                       "the shop will provide a diagnostic assessment within 24-48 hours. Repair estimates " +
                       "are subject to change upon further inspection. Parts ordered are non-refundable once " +
                       "sourced. The shop is not liable for pre-existing damage or data loss during repair. " +
                       "Devices not collected within 30 days of notification may be subject to storage fees.\n\n" +
                       "24-Hour Cancellation Policy: Customers may cancel or modify a service request without " +
                       "charge if done within 24 hours of submission. After this period, a diagnostic fee may " +
                       "apply. Cancellations must be made in writing or via phone.",
                UseMnemonic = false,
                Font = new Font("Segoe UI", TermsFontSize),
                ForeColor = SubtleGray,
                Location = new Point(CardPad, SectionHeaderHeight),
                AutoSize = true,
                MaximumSize = new Size(width - CardPad * 2, 0)
            };
            _termsCard.Controls.Add(_termsText);

            chkAcceptTerms = new CheckBox
            {
                Text = "Customer has read and accepted the terms",
                UseMnemonic = false,
                Font = new Font("Segoe UI", CheckboxFontSize),
                ForeColor = TitleColor,
                AutoSize = true,
                Location = new Point(CardPad, _termsText.Bottom + 16)
            };
            chkAcceptTerms.CheckedChanged += (s, e) =>
            {
                btnSubmit.Enabled = chkAcceptTerms.Checked;
                btnSubmit.BackColor = chkAcceptTerms.Checked ? Accent : DisabledGray;
                btnSubmit.HoverColor = chkAcceptTerms.Checked ? AccentHover : DisabledGrayHover;
                btnSubmit.Invalidate();
                if (chkAcceptTerms.Checked) lblError.Visible = false;
            };
            _termsCard.Controls.Add(chkAcceptTerms);

            lblError = new Label
            {
                ForeColor = Color.FromArgb(200, 70, 70),
                UseMnemonic = false,
                Font = new Font("Segoe UI", ErrorFontSize),
                AutoSize = false,
                Size = new Size(width - CardPad * 2, 24),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(CardPad, chkAcceptTerms.Bottom + 8),
                Visible = false
            };
            _termsCard.Controls.Add(lblError);

            SyncTermsCardHeight();
            return _termsCard;
        }

        /// <summary>Re-wraps the terms paragraph to the card's current width and
        /// repositions the checkbox/error label beneath it, then resizes the card
        /// to fit. Called from ReflowCards whenever the dialog is resized.</summary>
        private void SyncTermsCardHeight()
        {
            _termsText.MaximumSize = new Size(Math.Max(120, _termsCard.Width - CardPad * 2), 0);
            chkAcceptTerms.Location = new Point(CardPad, _termsText.Bottom + 16);
            lblError.Width = Math.Max(0, _termsCard.Width - CardPad * 2);
            lblError.Location = new Point(CardPad, chkAcceptTerms.Bottom + 8);
            _termsCard.Height = lblError.Bottom + 20;
        }

        private static void MakeRound(Control control)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(0, 0, control.Width, control.Height);
            control.Region = new Region(path);
        }

        private static readonly System.Text.RegularExpressions.Regex NamePattern = new(@"^[A-Za-z][A-Za-z\s'.-]*$");
        private static readonly System.Text.RegularExpressions.Regex PhonePattern = new(@"^[0-9][0-9\s\-+()]*$");
        private static readonly System.Text.RegularExpressions.Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

        private void BtnSubmit_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FirstNameValue) || string.IsNullOrWhiteSpace(LastNameValue) ||
                string.IsNullOrWhiteSpace(PhoneValue) || string.IsNullOrWhiteSpace(DeviceNameValue) ||
                string.IsNullOrWhiteSpace(ConcernValue))
            {
                lblError.Text = "Please fill in first name, last name, phone, device name, and concern.";
                lblError.Visible = true;
                return;
            }

            if (!NamePattern.IsMatch(FirstNameValue) || !NamePattern.IsMatch(LastNameValue) ||
                (MiddleNameValue.Length > 0 && !NamePattern.IsMatch(MiddleNameValue)))
            {
                lblError.Text = "Names can only contain letters — no numbers or symbols.";
                lblError.Visible = true;
                return;
            }

            if (!PhonePattern.IsMatch(PhoneValue))
            {
                lblError.Text = "Phone number can only contain digits — no letters.";
                lblError.Visible = true;
                return;
            }

            if (EmailValue.Length > 0 && !EmailPattern.IsMatch(EmailValue))
            {
                lblError.Text = "Enter a valid email address, e.g. customer@email.com.";
                lblError.Visible = true;
                return;
            }

            if (PromotionIdValue.HasValue && txtDiscount.Value.Trim().Length > 0 &&
                (!decimal.TryParse(txtDiscount.Value.Trim(), out var discount) || discount < 0))
            {
                lblError.Text = "Enter a valid, non-negative discount amount — or leave it blank for no discount.";
                lblError.Visible = true;
                return;
            }

            if (EstimatedCostValue <= 0)
            {
                lblError.Text = "Select at least one service/part, or the estimated cost can't be ₱0.";
                lblError.Visible = true;
                return;
            }

            if (!chkAcceptTerms.Checked)
            {
                lblError.Text = "The customer must accept the Terms & Conditions to continue.";
                lblError.Visible = true;
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
