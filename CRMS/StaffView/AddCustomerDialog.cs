using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CRMS.winforms; // reuses RoundedPanel/RoundedButton/RoundedTextBox from Login.cs
using CRMS.winforms.Shared;

namespace CRMS.winforms.StaffView
{
    /// <summary>
    /// Popup shown when Staff clicks "+ Add Customer": collects the walk-in
    /// customer's details, the device concern, an estimated service fee built
    /// from the parts/services picked, and requires the 24-hour-no-cancellation
    /// terms to be accepted before the ticket can be submitted. Styled to
    /// match the original compact card design (teal accent, plain totals,
    /// small inline section icons).
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

        private static readonly (string Name, decimal Price)[] ServiceOptions =
        {
            ("Diagnostic Fee", 25.00m),
            ("RAM Module", 45.00m),
            ("SSD (256 GB)", 80.00m),
            ("Battery Replacement", 55.00m),
            ("Screen / Display", 120.00m),
            ("Thermal Paste", 15.00m),
            ("Keyboard Replacement", 90.00m),
            ("Charger / Adapter", 35.00m),
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

        private readonly HashSet<string> _selectedServices = new();

        public string FirstNameValue => txtFirstName.Value.Trim();
        public string MiddleNameValue => txtMiddleName.Value.Trim();
        public string LastNameValue => txtLastName.Value.Trim();
        public string PhoneValue => txtPhone.Value.Trim();
        public string EmailValue => txtEmail.Value.Trim();
        public string DeviceNameValue => txtDeviceName.Value.Trim();
        public string ConcernValue => txtConcern.Value.Trim();
        public decimal EstimatedCostValue => _selectedServices.Sum(name => ServiceOptions.First(o => o.Name == name).Price);

        public AddCustomerDialog()
        {
            BuildLayout();
        }

        private void BuildLayout()
        {
            Text = "Add New Customer";
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(620, 720);
            MinimumSize = new Size(640, 580);
            WindowState = FormWindowState.Maximized; // opens covering the full screen — more room, less scrolling to reach the checkbox
            BackColor = PageBg;
            Font = new Font("Segoe UI", 9F);

            // ===================== Header =====================
            var header = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = CardColor };
            header.Controls.Add(new Label
            {
                Text = "Add New Customer",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = TitleColor,
                AutoSize = true,
                Location = new Point(24, 12)
            });
            header.Controls.Add(new Label
            {
                Text = "Fill in all sections to generate a ticket",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(24, 36)
            });
            header.Paint += (s, e) =>
            {
                using var pen = new Pen(CardBorder, 1);
                e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
            };
            Controls.Add(header);

            // ===================== Footer (Cancel / Confirm). Kept as a
            // fixed bar rather than embedded in the last card like the
            // original — so it's always reachable without scrolling. =====
            const int footerHeight = 76;
            const int footerPad = 16;
            const int btnHeight = 44;
            const int btnGap = 12;
            const int cancelWidth = 110;

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
                CornerRadius = 0, // square, not pill-shaped
                BackColor = Color.White,
                HoverColor = Color.FromArgb(245, 246, 248),
                ForeColor = TitleColor,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            footer.Controls.Add(btnCancel);

            btnSubmit = new RoundedButton
            {
                Text = "Confirm & Generate Ticket",
                Location = new Point(footerPad + cancelWidth + btnGap, btnY),
                Size = new Size(ClientSize.Width - footerPad * 2 - cancelWidth - btnGap, btnHeight),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                CornerRadius = 0, // square, not pill-shaped
                BackColor = DisabledGray,
                HoverColor = DisabledGrayHover,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Enabled = false
            };
            btnSubmit.Click += BtnSubmit_Click;
            footer.Controls.Add(btnSubmit);

            // ===================== Terms & Agreement — fixed, not inside the
            // scroll area, so the required checkbox is ALWAYS on screen and
            // can never end up scrolled out of view. Still its own labeled
            // "Terms & Agreement" section, not a bare checkbox. =====
            var termsSection = new Panel
            {
                Dock = DockStyle.Bottom,
                BackColor = PageBg,
                Padding = new Padding(16, 10, 4, 6)
            };

            int termsCardWidth = ClientSize.Width - termsSection.Padding.Horizontal;
            var termsCard = BuildTermsCard(termsCardWidth);
            termsCard.Location = new Point(0, 0);
            termsCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            termsSection.Controls.Add(termsCard);
            termsSection.Height = termsCard.Height + termsSection.Padding.Vertical;

            // Dock=Bottom siblings stack in reverse add-order (last added
            // ends up truly at the bottom edge) — add termsSection first so
            // footer, added after, is the one that lands at the very bottom.
            Controls.Add(termsSection);
            Controls.Add(footer);

            // ===================== Scrollable content (everything else) =====================
            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = PageBg,
                Padding = new Padding(16, 8, 4, 12)
            };
            Controls.Add(scroll);

            int cardWidth = ClientSize.Width - scroll.Padding.Horizontal;

            var customerCard = BuildCustomerCard(cardWidth);
            var deviceCard = BuildDeviceCard(cardWidth);
            var serviceCard = BuildServiceCard(cardWidth);
            var cards = new[] { customerCard, deviceCard, serviceCard };

            int y = 0;
            foreach (var card in cards)
            {
                card.Location = new Point(0, y);
                scroll.Controls.Add(card);
                y += card.Height + 12;
            }

            // Anchor alone was leaving a gap on the right — a vertical
            // scrollbar's actual on-screen width isn't reliably reflected by
            // the AutoScroll panel's ClientSize in time for Anchor math to
            // react to it. Instead, measure the panel's real available width
            // directly on every resize and apply it to each card ourselves;
            // each card's own children are still Anchored (a plain, non-
            // scrolling parent), so they correctly reflow once the card's
            // Width actually changes.
            void ReflowCardWidths()
            {
                int availableWidth = scroll.ClientSize.Width - scroll.Padding.Horizontal;
                if (availableWidth <= 0) return;
                foreach (var card in cards)
                    card.Width = availableWidth;
            }
            scroll.Resize += (s, e) => ReflowCardWidths();
            ReflowCardWidths();
        }

        private RoundedPanel NewCard(int width)
        {
            return new RoundedPanel { Width = width, BackColor = CardColor, CornerRadius = 16, BorderColor = CardBorder };
        }

        private void AddSectionHeader(RoundedPanel card, string icon, string title)
        {
            var iconBadge = new Panel { Size = new Size(30, 30), Location = new Point(20, 18), BackColor = AccentSoftBg };
            MakeRound(iconBadge);
            card.Controls.Add(iconBadge);
            iconBadge.Controls.Add(new Label
            {
                Text = icon,
                Font = new Font("Segoe UI Emoji", 9.5F),
                Size = new Size(30, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            });

            var titleLbl = new Label
            {
                Text = title,
                UseMnemonic = false,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = TitleColor,
                AutoSize = true
            };
            card.Controls.Add(titleLbl);
            titleLbl.Location = new Point(60, 18 + (30 - titleLbl.PreferredHeight) / 2);
        }

        private void AddLabeledField(RoundedPanel card, out RoundedTextBox box, string caption, string placeholder,
            int x, int y, int width, bool stretchWithCard = false)
        {
            card.Controls.Add(new Label
            {
                Text = caption,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(x, y)
            });
            box = new RoundedTextBox
            {
                Location = new Point(x, y + 18),
                Size = new Size(width, 36),
                Placeholder = placeholder
            };
            if (stretchWithCard)
                box.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            card.Controls.Add(box);
        }

        // ----- Customer Information -----
        private RoundedPanel BuildCustomerCard(int width)
        {
            var card = NewCard(width);
            AddSectionHeader(card, "👤", "Customer Information");

            int fieldsTop = 62;
            int gap = 16;
            int triWidth = (width - 48 - gap * 2) / 3;
            int lastTriWidth = (width - 48) - triWidth * 2 - gap * 2;
            int halfWidth = (width - 48 - gap) / 2;

            AddLabeledField(card, out txtFirstName, "FIRST NAME", "e.g. Maria", 24, fieldsTop, triWidth);
            AddLabeledField(card, out txtMiddleName, "MIDDLE NAME", "e.g. Anne", 24 + triWidth + gap, fieldsTop, triWidth);
            AddLabeledField(card, out txtLastName, "LAST NAME", "e.g. Santos", 24 + (triWidth + gap) * 2, fieldsTop, lastTriWidth, stretchWithCard: true);

            AddLabeledField(card, out txtPhone, "PHONE NUMBER", "+1 (555) 000-0000", 24, fieldsTop + 62, halfWidth);
            AddLabeledField(card, out txtEmail, "EMAIL ADDRESS", "customer@email.com", 24 + halfWidth + gap, fieldsTop + 62, halfWidth, stretchWithCard: true);

            card.Height = fieldsTop + 62 + 56 + 20;
            return card;
        }

        // ----- Device Information -----
        private RoundedPanel BuildDeviceCard(int width)
        {
            var card = NewCard(width);
            AddSectionHeader(card, "💻", "Device Information");

            int fieldsTop = 62;
            AddLabeledField(card, out txtDeviceName, "DEVICE NAME / MODEL", "e.g. MacBook Pro 14\" M3 (2023)", 24, fieldsTop, width - 48, stretchWithCard: true);

            card.Controls.Add(new Label
            {
                Text = "CUSTOMER'S CONCERN",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(24, fieldsTop + 62)
            });
            txtConcern = new RoundedMultilineBox
            {
                Location = new Point(24, fieldsTop + 80),
                Size = new Size(width - 48, 72),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Placeholder = "Describe the issue in the customer's own words..."
            };
            card.Controls.Add(txtConcern);

            card.Height = fieldsTop + 62 + 72 + 20;
            return card;
        }

        // ----- Repair Parts & Tools -----
        private RoundedPanel BuildServiceCard(int width)
        {
            var card = NewCard(width);
            AddSectionHeader(card, "🔧", "Repair Parts & Tools");

            int px = 24, py = 62;
            const int chipHeight = 52;

            foreach (var option in ServiceOptions)
            {
                var chip = MakeServiceChip(option.Name, option.Price);
                if (px + chip.Width > width - 24)
                {
                    px = 24;
                    py += chipHeight + 10;
                }
                chip.Location = new Point(px, py);
                card.Controls.Add(chip);
                px += chip.Width + 10;
            }

            py += chipHeight + 16;

            var totalStrip = new RoundedPanel
            {
                Location = new Point(24, py),
                Size = new Size(width - 48, 56),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = FieldFill,
                CornerRadius = 12
            };
            totalStrip.Resize += (s, e) => PositionTotalValue(totalStrip);
            card.Controls.Add(totalStrip);

            totalStrip.Controls.Add(new Label
            {
                Text = "ESTIMATED TOTAL",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(16, 10)
            });
            lblSelectedCount = new Label
            {
                Text = "0 items selected",
                Font = new Font("Segoe UI", 8F),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(16, 30)
            };
            totalStrip.Controls.Add(lblSelectedCount);

            lblTotalValue = new Label
            {
                Text = 0m.ToString("$0.00"),
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Accent,
                AutoSize = true
            };
            totalStrip.Controls.Add(lblTotalValue);
            PositionTotalValue(totalStrip);

            card.Height = py + 56 + 20;
            return card;
        }

        private void PositionTotalValue(RoundedPanel totalStrip)
        {
            lblTotalValue.Location = new Point(totalStrip.Width - lblTotalValue.PreferredWidth - 16, 14);
        }

        private RoundedButton MakeServiceChip(string name, decimal price)
        {
            var chip = new RoundedButton
            {
                Height = 52,
                BackColor = FieldFill,
                HoverColor = Color.FromArgb(240, 241, 245),
                ForeColor = TitleColor,
                Font = new Font("Segoe UI", 8F),
                CornerRadius = 12,
                Text = $"{name}\n{price:$0.00}"
            };
            int textWidth = TextRenderer.MeasureText(name, chip.Font).Width;
            int priceWidth = TextRenderer.MeasureText(price.ToString("$0.00"), new Font("Segoe UI", 7.5F)).Width;
            chip.Width = Math.Max(textWidth, priceWidth) + 36;
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
            chip.Font = new Font("Segoe UI", 8F, selected ? FontStyle.Bold : FontStyle.Regular);
            chip.Text = selected ? $"✓  {name}\n{option.Price:$0.00}" : $"{name}\n{option.Price:$0.00}";
            chip.Invalidate();

            decimal total = EstimatedCostValue;
            lblSelectedCount.Text = $"{_selectedServices.Count} item{(_selectedServices.Count == 1 ? "" : "s")} selected";
            lblTotalValue.Text = total.ToString("$0.00");
            PositionTotalValue((RoundedPanel)lblTotalValue.Parent!);
        }

        // ----- Terms & Agreement -----
        private RoundedPanel BuildTermsCard(int width)
        {
            var card = NewCard(width);
            AddSectionHeader(card, "📄", "Terms & Agreement");

            var termsBox = new RoundedPanel
            {
                Location = new Point(24, 62),
                Size = new Size(width - 48, 110),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = true, // safety net: if the text ever needs more room than fits, it scrolls instead of silently clipping
                BackColor = FieldFill,
                CornerRadius = 12
            };
            card.Controls.Add(termsBox);
            termsBox.Controls.Add(new Label
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
                Font = new Font("Segoe UI", 7.7F),
                ForeColor = SubtleGray,
                Location = new Point(14, 10),
                Size = new Size(width - 48 - 28, 100),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            });

            chkAcceptTerms = new CheckBox
            {
                Text = "Customer has read and accepted the terms",
                UseMnemonic = false,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = TitleColor,
                AutoSize = true,
                Location = new Point(24, 182)
            };
            chkAcceptTerms.CheckedChanged += (s, e) =>
            {
                btnSubmit.Enabled = chkAcceptTerms.Checked;
                btnSubmit.BackColor = chkAcceptTerms.Checked ? Accent : DisabledGray;
                btnSubmit.HoverColor = chkAcceptTerms.Checked ? AccentHover : DisabledGrayHover;
                btnSubmit.Invalidate();
            };
            card.Controls.Add(chkAcceptTerms);

            lblError = new Label
            {
                ForeColor = Color.FromArgb(200, 70, 70),
                UseMnemonic = false,
                Font = new Font("Segoe UI", 8F),
                AutoSize = false,
                Size = new Size(width - 48, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(24, 204),
                Visible = false
            };
            card.Controls.Add(lblError);

            card.Height = 204 + 20 + 16;
            return card;
        }

        private static void MakeRound(Control control)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(0, 0, control.Width, control.Height);
            control.Region = new Region(path);
        }

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
