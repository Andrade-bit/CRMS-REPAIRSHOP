using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using CRMS.winforms; // reuses RoundedPanel/RoundedButton/RoundedTextBox from Login.cs
using CRMS.winforms.Controllers;
using CRMS.winforms.Shared;

namespace CRMS.winforms.StaffView
{
    /// <summary>
    /// Popup shown when Staff clicks "Edit" on a pending ticket: lets them fix the
    /// customer's name/phone/email, the device name/concern, which repair parts are
    /// checked, and the estimated cost. Same card-based visual language as
    /// AddCustomerDialog (icons, colors, fonts, chip picker).
    ///
    /// Unlike AddCustomerDialog, the cost box here stays manually editable even
    /// though chip toggles also update it — tickets created before this feature
    /// existed have no recorded parts, so their real cost must never be silently
    /// zeroed out just because no chips happen to be checked.
    /// </summary>
    public class EditTicketDialog : Form
    {
        // ----- Colors (matches AddCustomerDialog) -----
        private static readonly Color CardColor = Color.White;
        private static readonly Color CardBorder = Color.FromArgb(232, 234, 238);
        private static readonly Color TitleColor = Color.FromArgb(33, 37, 41);
        private static readonly Color SubtleGray = Color.FromArgb(140, 144, 153);
        private static readonly Color Accent = Color.FromArgb(58, 150, 150);
        private static readonly Color AccentHover = Color.FromArgb(47, 130, 130);
        private static readonly Color AccentSoftBg = Color.FromArgb(230, 244, 244);
        private static readonly Color FieldFill = Color.FromArgb(249, 249, 251);
        private static readonly Color PageBg = Color.FromArgb(237, 239, 242);

        private const int CardPad = 24;
        private const int SectionHeaderHeight = 68;
        private const int FieldBoxHeight = 42;
        private const int FieldBlockHeight = 74;
        private const int FieldCaptionGap = 22;
        private const float CaptionFontSize = 9.5F;
        private const float InputFontSize = 11.5F;
        private const float ChipFontSize = 10F;

        // Same catalog as AddCustomerDialog — kept in sync manually since each
        // dialog is self-contained (matches the existing pattern in this file pair).
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

        private RoundedTextBox txtFullName = null!;
        private RoundedTextBox txtPhone = null!;
        private RoundedTextBox txtEmail = null!;
        private RoundedTextBox txtDeviceName = null!;
        private RoundedMultilineBox txtConcern = null!;
        private RoundedTextBox txtEstimatedCost = null!;
        private Label lblSelectedCount = null!;
        private RoundedButton btnSave = null!;

        private RoundedPanel _serviceCard = null!;
        private FlowLayoutPanel _chipFlow = null!;
        private RoundedPanel _totalStrip = null!;
        private readonly HashSet<string> _selectedServices = new();

        private readonly Func<System.Threading.Tasks.Task<List<CRMS.winforms.Api.PromotionDto>>>? _getApprovedPromotions;
        private readonly int? _currentPromotionId;
        private readonly string? _currentPromotionTitle;
        private readonly decimal? _currentOriginalPrice;
        private ComboBox cboPromotion = null!;
        private Label lblPromotionCaption = null!;
        private Label lblOriginalPriceCaption = null!;
        private RoundedTextBox txtOriginalPrice = null!;

        private readonly record struct PromotionOption(int? PromotionId, string Display)
        {
            public override string ToString() => Display;
        }

        public string FullNameValue => txtFullName.Value.Trim();
        public string PhoneValue => txtPhone.Value.Trim();
        public string EmailValue => txtEmail.Value.Trim();
        public string DeviceNameValue => txtDeviceName.Value.Trim();
        public string ConcernValue => txtConcern.Value.Trim();
        public decimal EstimatedCostValue { get; private set; }
        public IReadOnlyList<TicketPart> SelectedPartsValue =>
            _selectedServices.Select(name => new TicketPart(name, ServiceOptions.First(o => o.Name == name).Price)).ToList();

        /// <summary>Null means "no promotion" — check ClearedPromotion to tell that
        /// apart from "unchanged" (the picker wasn't available at all).</summary>
        public int? PromotionIdValue => (cboPromotion?.SelectedItem as PromotionOption?)?.PromotionId;

        /// <summary>True when the picker was available and the customer's original
        /// promotion (if any) was explicitly changed to "No promotion applied".</summary>
        public bool ClearedPromotion => _getApprovedPromotions != null && _currentPromotionId.HasValue && PromotionIdValue is null;

        /// <summary>The price before the promotion's discount — only meaningful
        /// (and only sent to the API) alongside a real PromotionIdValue.</summary>
        public decimal? OriginalPriceValue
        {
            get
            {
                if (PromotionIdValue is null || txtOriginalPrice is null) return null;
                return decimal.TryParse(txtOriginalPrice.Value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) && amount > 0
                    ? amount : (decimal?)null;
            }
        }

        public EditTicketDialog(string fullName, string phone, string email,
            string deviceName, string concern, decimal estimatedCost, IEnumerable<TicketPart>? selectedParts = null,
            Func<System.Threading.Tasks.Task<List<CRMS.winforms.Api.PromotionDto>>>? getApprovedPromotions = null,
            int? currentPromotionId = null, string? currentPromotionTitle = null, decimal? currentOriginalPrice = null)
        {
            foreach (var part in selectedParts ?? Enumerable.Empty<TicketPart>())
                if (ServiceOptions.Any(o => o.Name == part.Name))
                    _selectedServices.Add(part.Name);

            _getApprovedPromotions = getApprovedPromotions;
            _currentPromotionId = currentPromotionId;
            _currentPromotionTitle = currentPromotionTitle;
            _currentOriginalPrice = currentOriginalPrice;

            BuildLayout(fullName, phone, email, deviceName, concern, estimatedCost);

            if (_getApprovedPromotions != null)
                Load += async (s, e) => await LoadApprovedPromotionsAsync();
        }

        private void BuildLayout(string fullName, string phone, string email,
            string deviceName, string concern, decimal estimatedCost)
        {
            Text = "Edit Ticket";
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(680, 800);
            MinimumSize = new Size(600, 560);
            BackColor = PageBg;
            Font = new Font("Segoe UI", 9F);

            var header = BuildHeader();
            var footer = BuildFooter();
            var scroll = BuildScrollArea(fullName, phone, email, deviceName, concern, estimatedCost);

            // Fill first, footer next, header last (same load-bearing order as AddCustomerDialog).
            Controls.Add(scroll);
            Controls.Add(footer);
            Controls.Add(header);
        }

        private Panel BuildHeader()
        {
            var header = new Panel { Dock = DockStyle.Top, Height = 84, BackColor = CardColor };

            var title = new Label
            {
                Text = "Edit Ticket",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = TitleColor,
                AutoSize = true,
                Location = new Point(CardPad, 18)
            };
            header.Controls.Add(title);

            header.Controls.Add(new Label
            {
                Text = "Fix the customer's details, the device, or the repair parts",
                Font = new Font("Segoe UI", 10.5F),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(CardPad, 18 + title.PreferredHeight + 6)
            });

            header.Paint += (s, e) =>
            {
                using var pen = new Pen(CardBorder, 1);
                e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
            };
            return header;
        }

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
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold)
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            footer.Controls.Add(btnCancel);

            btnSave = new RoundedButton
            {
                Text = "Save Changes",
                Location = new Point(footerPad + cancelWidth + btnGap, btnY),
                Size = new Size(300, btnHeight),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                CornerRadius = 0,
                BackColor = Accent,
                HoverColor = AccentHover,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold)
            };
            btnSave.Click += BtnSave_Click;
            footer.Controls.Add(btnSave);
            AcceptButton = btnSave;

            footer.Resize += (s, e) =>
                btnSave.Width = Math.Max(160, footer.ClientSize.Width - footerPad * 2 - cancelWidth - btnGap);

            return footer;
        }

        private Panel _scroll = null!;
        private RoundedPanel[] _cards = Array.Empty<RoundedPanel>();

        private Panel BuildScrollArea(string fullName, string phone, string email,
            string deviceName, string concern, decimal estimatedCost)
        {
            _scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = PageBg,
                Padding = new Padding(16, 12, 4, 24)
            };

            int cardWidth = 620;
            _cards = new[]
            {
                BuildCustomerCard(cardWidth, fullName, phone, email),
                BuildDeviceCard(cardWidth, deviceName, concern),
                BuildServiceCard(cardWidth, estimatedCost)
            };
            foreach (var card in _cards) _scroll.Controls.Add(card);

            _scroll.Resize += (s, e) => ReflowCards();
            ReflowCards();
            return _scroll;
        }

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
                card.Location = new Point(_scroll.Padding.Left, y);
                y += card.Height + 12;
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
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(0, 0, iconBadge.Width, iconBadge.Height);
            iconBadge.Region = new Region(path);
            card.Controls.Add(iconBadge);
            iconBadge.Controls.Add(new Label
            {
                Text = icon,
                Font = new Font("Segoe UI Emoji", 13F),
                Size = new Size(36, 36),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            });

            var titleLbl = new Label
            {
                Text = title,
                UseMnemonic = false,
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                ForeColor = TitleColor,
                AutoSize = true
            };
            card.Controls.Add(titleLbl);
            titleLbl.Location = new Point(66, 18 + (36 - titleLbl.PreferredHeight) / 2);
        }

        private void AddLabeledField(RoundedPanel card, out RoundedTextBox box, string caption, string initialValue,
            int x, int y, int width, bool stretchWithCard = false)
        {
            card.Controls.Add(new Label
            {
                Text = caption,
                Font = new Font("Segoe UI", CaptionFontSize, FontStyle.Bold),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(x, y)
            });
            box = new RoundedTextBox
            {
                Location = new Point(x, y + FieldCaptionGap),
                Size = new Size(width, FieldBoxHeight),
                TextSize = InputFontSize,
                Value = initialValue
            };
            if (stretchWithCard)
                box.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            card.Controls.Add(box);
        }

        // ----- Customer Information -----
        private RoundedPanel BuildCustomerCard(int width, string fullName, string phone, string email)
        {
            var card = NewCard(width);
            AddSectionHeader(card, "\U0001F464", "Customer Information");

            int fieldsTop = SectionHeaderHeight;
            AddLabeledField(card, out txtFullName, "FULL NAME", fullName, CardPad, fieldsTop, width - CardPad * 2, stretchWithCard: true);

            int gap = 16;
            int halfWidth = (width - CardPad * 2 - gap) / 2;
            AddLabeledField(card, out txtPhone, "PHONE NUMBER", phone, CardPad, fieldsTop + FieldBlockHeight, halfWidth);
            AddLabeledField(card, out txtEmail, "EMAIL ADDRESS", email, CardPad + halfWidth + gap, fieldsTop + FieldBlockHeight, halfWidth, stretchWithCard: true);

            card.Height = fieldsTop + FieldBlockHeight + FieldCaptionGap + FieldBoxHeight + 20;
            return card;
        }

        // ----- Device Information -----
        private RoundedPanel BuildDeviceCard(int width, string deviceName, string concern)
        {
            var card = NewCard(width);
            AddSectionHeader(card, "\U0001F4BB", "Device Information");

            int fieldsTop = SectionHeaderHeight;
            AddLabeledField(card, out txtDeviceName, "DEVICE NAME / MODEL", deviceName, CardPad, fieldsTop, width - CardPad * 2, stretchWithCard: true);

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
                TextSize = InputFontSize,
                Value = concern
            };
            card.Controls.Add(txtConcern);

            card.Height = fieldsTop + FieldBlockHeight + FieldCaptionGap + 84 + 20;
            return card;
        }

        // ----- Repair Parts & Tools -----
        private RoundedPanel BuildServiceCard(int width, decimal estimatedCost)
        {
            _serviceCard = NewCard(width);
            AddSectionHeader(_serviceCard, "\U0001F527", "Repair Parts & Tools");

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
                Size = new Size(width - CardPad * 2, 64),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = FieldFill,
                CornerRadius = 12
            };
            _serviceCard.Controls.Add(_totalStrip);

            _totalStrip.Controls.Add(new Label
            {
                Text = "ESTIMATED COST",
                Font = new Font("Segoe UI", CaptionFontSize, FontStyle.Bold),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(16, 10)
            });
            int selectedCount = _selectedServices.Count;
            lblSelectedCount = new Label
            {
                Text = $"{selectedCount} item{(selectedCount == 1 ? "" : "s")} checked — cost stays editable below",
                Font = new Font("Segoe UI", 9F),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(16, 30)
            };
            _totalStrip.Controls.Add(lblSelectedCount);

            txtEstimatedCost = new RoundedTextBox
            {
                Size = new Size(140, 40),
                TextSize = 13F,
                Value = estimatedCost.ToString("0.00", CultureInfo.InvariantCulture)
            };
            _totalStrip.Controls.Add(txtEstimatedCost);
            _totalStrip.Resize += (s, e) => PositionCostBox();
            PositionCostBox();

            if (_getApprovedPromotions != null)
            {
                lblPromotionCaption = new Label
                {
                    Text = "APPLY PROMOTION (OPTIONAL)",
                    Font = new Font("Segoe UI", CaptionFontSize, FontStyle.Bold),
                    ForeColor = SubtleGray,
                    AutoSize = true
                };
                _serviceCard.Controls.Add(lblPromotionCaption);

                cboPromotion = new ComboBox
                {
                    Size = new Size(width - CardPad * 2, FieldBoxHeight),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    Font = new Font("Segoe UI", InputFontSize),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    FlatStyle = FlatStyle.Flat
                };
                cboPromotion.Items.Add(new PromotionOption(null, "No promotion applied"));
                if (_currentPromotionId.HasValue && !string.IsNullOrEmpty(_currentPromotionTitle))
                    cboPromotion.Items.Add(new PromotionOption(_currentPromotionId, _currentPromotionTitle));
                cboPromotion.SelectedIndex = cboPromotion.Items.Count - 1;
                _serviceCard.Controls.Add(cboPromotion);

                bool startsWithPromotion = _currentPromotionId.HasValue;
                lblOriginalPriceCaption = new Label
                {
                    Text = "ORIGINAL PRICE BEFORE DISCOUNT (₱)",
                    Font = new Font("Segoe UI", CaptionFontSize, FontStyle.Bold),
                    ForeColor = SubtleGray,
                    AutoSize = true,
                    Visible = startsWithPromotion
                };
                _serviceCard.Controls.Add(lblOriginalPriceCaption);

                txtOriginalPrice = new RoundedTextBox
                {
                    Size = new Size(width - CardPad * 2, FieldBoxHeight),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    Placeholder = "0.00",
                    TextSize = InputFontSize,
                    Value = _currentOriginalPrice.HasValue ? _currentOriginalPrice.Value.ToString("0.00", CultureInfo.InvariantCulture) : "",
                    Visible = startsWithPromotion
                };
                _serviceCard.Controls.Add(txtOriginalPrice);

                // Only meaningful alongside an actual promotion — hide (and blank)
                // it the moment the ticket goes back to having none, so a stale
                // number can never linger and get sent for a plain ticket.
                cboPromotion.SelectedIndexChanged += (s, e) =>
                {
                    bool hasPromotion = PromotionIdValue.HasValue;
                    lblOriginalPriceCaption.Visible = hasPromotion;
                    txtOriginalPrice.Visible = hasPromotion;
                    if (!hasPromotion) txtOriginalPrice.Value = "";
                    SyncServiceCardHeight();
                };
            }

            _chipFlow.SizeChanged += (s, e) => SyncServiceCardHeight();
            SyncServiceCardHeight();

            return _serviceCard;
        }

        private async System.Threading.Tasks.Task LoadApprovedPromotionsAsync()
        {
            if (_getApprovedPromotions is null || cboPromotion is null) return;

            var currentSelection = (PromotionOption?)cboPromotion.SelectedItem;
            var promotions = await _getApprovedPromotions();

            foreach (var promo in promotions)
            {
                if (promo.PromotionId == _currentPromotionId) continue; // already added, pre-selected
                cboPromotion.Items.Add(new PromotionOption(promo.PromotionId, promo.Title));
            }

            if (currentSelection.HasValue)
            {
                for (int i = 0; i < cboPromotion.Items.Count; i++)
                {
                    if (((PromotionOption)cboPromotion.Items[i]).PromotionId == currentSelection.Value.PromotionId)
                    {
                        cboPromotion.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void PositionCostBox()
        {
            txtEstimatedCost.Location = new Point(
                _totalStrip.Width - txtEstimatedCost.Width - 16,
                _totalStrip.Height / 2 - txtEstimatedCost.Height / 2);
        }

        private void SyncServiceCardHeight()
        {
            _totalStrip.Top = _chipFlow.Bottom + 16;

            int bottomContentY = _totalStrip.Bottom;
            if (cboPromotion != null)
            {
                lblPromotionCaption.Location = new Point(CardPad, _totalStrip.Bottom + 16);
                cboPromotion.Location = new Point(CardPad, _totalStrip.Bottom + 38);
                bottomContentY = cboPromotion.Bottom;

                if (txtOriginalPrice.Visible)
                {
                    lblOriginalPriceCaption.Location = new Point(CardPad, cboPromotion.Bottom + 16);
                    txtOriginalPrice.Location = new Point(CardPad, cboPromotion.Bottom + 38);
                    bottomContentY = txtOriginalPrice.Bottom;
                }
            }

            _serviceCard.Height = bottomContentY + 20;
            ReflowCards();
        }

        private RoundedButton MakeServiceChip(string name, decimal price)
        {
            var font = new Font("Segoe UI", ChipFontSize);
            bool selected = _selectedServices.Contains(name);
            var chip = new RoundedButton
            {
                Height = 58,
                Margin = new Padding(0, 0, 10, 10),
                BackColor = selected ? AccentSoftBg : FieldFill,
                HoverColor = selected ? AccentSoftBg : Color.FromArgb(240, 241, 245),
                ForeColor = selected ? Accent : TitleColor,
                Font = new Font("Segoe UI", ChipFontSize, selected ? FontStyle.Bold : FontStyle.Regular),
                CornerRadius = 12,
                Text = selected ? $"✓  {name}\n{price:₱#,##0.00}" : $"{name}\n{price:₱#,##0.00}"
            };
            int textWidth = TextRenderer.MeasureText(name, font).Width;
            int priceWidth = TextRenderer.MeasureText(price.ToString("₱#,##0.00"), font).Width;
            chip.Width = Math.Max(textWidth, priceWidth) + 52;
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

            int count = _selectedServices.Count;
            lblSelectedCount.Text = $"{count} item{(count == 1 ? "" : "s")} checked — cost stays editable below";

            // Auto-fill the cost box from the checked chips, but it's still a real
            // text box afterward — staff can override it if the chips don't match.
            decimal total = _selectedServices.Sum(n => ServiceOptions.First(o => o.Name == n).Price);
            txtEstimatedCost.Value = total.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FullNameValue) || string.IsNullOrWhiteSpace(PhoneValue) ||
                string.IsNullOrWhiteSpace(DeviceNameValue) || string.IsNullOrWhiteSpace(ConcernValue))
            {
                ShowError("Please fill in the customer's name, phone, device name, and concern.");
                return;
            }

            if (!decimal.TryParse(txtEstimatedCost.Value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var cost) || cost < 0)
            {
                ShowError("Enter a valid, non-negative estimated cost.");
                return;
            }

            if (PromotionIdValue.HasValue && txtOriginalPrice.Visible && !string.IsNullOrWhiteSpace(txtOriginalPrice.Value) &&
                (!decimal.TryParse(txtOriginalPrice.Value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var originalPrice) || originalPrice < cost))
            {
                ShowError("Original price can't be less than the estimated cost — leave it blank if there's no discount to record.");
                return;
            }

            EstimatedCostValue = cost;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ShowError(string message)
        {
            MessageBox.Show(this, message, "Check the form", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
