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
    /// terms to be accepted before the ticket can be submitted.
    /// </summary>
    public class AddCustomerDialog : Form
    {
        private static readonly Color CardBorder = Color.FromArgb(232, 234, 238);
        private static readonly Color FieldFill = Color.FromArgb(249, 249, 251);
        private static readonly Color DisabledGray = Color.FromArgb(163, 169, 181);
        private static readonly Color DisabledGrayHover = Color.FromArgb(150, 156, 168);
        private static readonly Color PageBg = Color.FromArgb(240, 242, 245);

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
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(680, 800);
            BackColor = PageBg;
            Font = new Font("Segoe UI", 9.5F);

            // ===================== Header =====================
            var header = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.White };
            header.Controls.Add(new Label
            {
                Text = "Add New Customer",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(28, 16)
            });
            header.Controls.Add(new Label
            {
                Text = "Walk-in intake — device details and estimated fee",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(28, 46)
            });
            header.Paint += (s, e) =>
            {
                using var pen = new Pen(CardBorder, 1);
                e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
            };
            Controls.Add(header);

            // ===================== Footer (sized once against the FORM's own
            // fixed ClientSize, not a child panel's — that mismatch was what
            // made Submit render clipped before) =====================
            const int footerHeight = 92;
            const int footerPad = 28;
            const int btnHeight = 48;
            const int btnGap = 16;
            const int cancelWidth = 130;

            var footer = new Panel { Dock = DockStyle.Bottom, Height = footerHeight, BackColor = Color.White };
            footer.Paint += (s, e) =>
            {
                using var pen = new Pen(CardBorder, 1);
                e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
            };

            int btnY = (footerHeight - btnHeight) / 2;
            int submitWidth = ClientSize.Width - footerPad * 2 - cancelWidth - btnGap;

            var btnCancel = new RoundedButton
            {
                Text = "Cancel",
                Location = new Point(footerPad, btnY),
                Size = new Size(cancelWidth, btnHeight),
                BackColor = Color.White,
                HoverColor = Color.FromArgb(245, 246, 248),
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                CornerRadius = 12
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            footer.Controls.Add(btnCancel);

            btnSubmit = new RoundedButton
            {
                Text = "Submit",
                Location = new Point(footerPad + cancelWidth + btnGap, btnY),
                Size = new Size(submitWidth, btnHeight),
                BackColor = DisabledGray,
                HoverColor = DisabledGrayHover,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                CornerRadius = 12,
                Enabled = false
            };
            btnSubmit.Click += BtnSubmit_Click;
            footer.Controls.Add(btnSubmit);

            Controls.Add(footer);

            // ===================== Scrollable content =====================
            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = PageBg,
                Padding = new Padding(28, 20, 28, 20)
            };
            Controls.Add(scroll);
            scroll.BringToFront();
            footer.BringToFront();

            int cardWidth = ClientSize.Width - 56 - SystemInformation.VerticalScrollBarWidth;

            var customerCard = BuildCustomerCard(cardWidth);
            var serviceCard = BuildServiceCard(cardWidth);
            var termsCard = BuildTermsCard(cardWidth);

            int y = 0;
            foreach (var card in new[] { customerCard, serviceCard, termsCard })
            {
                card.Location = new Point(0, y);
                scroll.Controls.Add(card);
                y += card.Height + 20;
            }
        }

        private RoundedPanel NewCard(int width)
        {
            return new RoundedPanel { Width = width, BackColor = Color.White, CornerRadius = 16, BorderColor = CardBorder };
        }

        private void AddSectionTitle(RoundedPanel card, string icon, string text)
        {
            var iconBadge = new Panel { Size = new Size(34, 34), Location = new Point(24, 20), BackColor = Theme.SelectionTint };
            MakeRound(iconBadge);
            card.Controls.Add(iconBadge);
            iconBadge.Controls.Add(new Label
            {
                Text = icon,
                Font = new Font("Segoe UI Emoji", 11F),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            });

            card.Controls.Add(new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(68, 27)
            });
        }

        private void AddField(RoundedPanel card, out RoundedTextBox box, string caption, string placeholder, int x, int y, int width, bool isPassword = false)
        {
            card.Controls.Add(new Label
            {
                Text = caption,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(x, y)
            });
            box = new RoundedTextBox
            {
                Location = new Point(x, y + 20),
                Size = new Size(width, 40),
                Placeholder = placeholder,
                IsPassword = isPassword
            };
            card.Controls.Add(box);
        }

        // ----- Customer + Device Information -----
        private RoundedPanel BuildCustomerCard(int width)
        {
            var card = NewCard(width);
            AddSectionTitle(card, "👤", "Customer & Device Information");

            int fieldsTop = 76;
            int gap = 18;
            int triWidth = (width - 48 - gap * 2) / 3;
            int lastTriWidth = (width - 48) - triWidth * 2 - gap * 2;
            int halfWidth = (width - 48 - gap) / 2;

            AddField(card, out txtFirstName, "FIRST NAME", "e.g. Maria", 24, fieldsTop, triWidth);
            AddField(card, out txtMiddleName, "MIDDLE NAME", "e.g. Anne", 24 + triWidth + gap, fieldsTop, triWidth);
            AddField(card, out txtLastName, "LAST NAME", "e.g. Santos", 24 + (triWidth + gap) * 2, fieldsTop, lastTriWidth);

            int row2 = fieldsTop + 70;
            AddField(card, out txtPhone, "PHONE NUMBER", "+1 (555) 000-0000", 24, row2, halfWidth);
            AddField(card, out txtEmail, "EMAIL ADDRESS", "customer@email.com", 24 + halfWidth + gap, row2, halfWidth);

            int row3 = row2 + 70;
            AddField(card, out txtDeviceName, "DEVICE NAME / MODEL", "e.g. MacBook Pro 14\" M3 (2023)", 24, row3, width - 48);

            int row4 = row3 + 70;
            card.Controls.Add(new Label
            {
                Text = "CUSTOMER'S CONCERN",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, row4)
            });
            txtConcern = new RoundedMultilineBox
            {
                Location = new Point(24, row4 + 20),
                Size = new Size(width - 48, 84),
                Placeholder = "Describe the issue in the customer's own words..."
            };
            card.Controls.Add(txtConcern);

            card.Height = row4 + 20 + 84 + 24;
            return card;
        }

        // ----- Estimated Service Fee -----
        private RoundedPanel BuildServiceCard(int width)
        {
            var card = NewCard(width);
            AddSectionTitle(card, "🔧", "Estimated Service Fee");

            int px = 24, py = 76;
            const int chipHeight = 54;

            foreach (var option in ServiceOptions)
            {
                var chip = MakeServiceChip(option.Name, option.Price);
                if (px + chip.Width > width - 24)
                {
                    px = 24;
                    py += chipHeight + 12;
                }
                chip.Location = new Point(px, py);
                card.Controls.Add(chip);
                px += chip.Width + 12;
            }

            py += chipHeight + 20;

            var totalStrip = new RoundedPanel
            {
                Location = new Point(24, py),
                Size = new Size(width - 48, 64),
                BackColor = FieldFill,
                CornerRadius = 14
            };
            card.Controls.Add(totalStrip);

            totalStrip.Controls.Add(new Label
            {
                Text = "ESTIMATED TOTAL",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(20, 12)
            });
            lblSelectedCount = new Label
            {
                Text = "0 items selected",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(20, 34)
            };
            totalStrip.Controls.Add(lblSelectedCount);

            lblTotalValue = new Label
            {
                Text = 0m.ToString("$0.00"),
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Theme.Accent,
                AutoSize = true
            };
            totalStrip.Controls.Add(lblTotalValue);
            lblTotalValue.Location = new Point(totalStrip.Width - lblTotalValue.PreferredWidth - 20, 16);

            card.Height = py + 64 + 24;
            return card;
        }

        private RoundedButton MakeServiceChip(string name, decimal price)
        {
            var chip = new RoundedButton
            {
                Height = 54,
                BackColor = FieldFill,
                HoverColor = Color.FromArgb(240, 241, 245),
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 8.75F),
                CornerRadius = 12,
                Text = $"{name}\n{price:$0.00}"
            };
            int textWidth = TextRenderer.MeasureText(name, chip.Font).Width;
            int priceWidth = TextRenderer.MeasureText(price.ToString("$0.00"), new Font("Segoe UI", 8F)).Width;
            chip.Width = Math.Max(textWidth, priceWidth) + 40;
            chip.Click += (s, e) => ToggleService(name, chip);
            return chip;
        }

        private void ToggleService(string name, RoundedButton chip)
        {
            var option = ServiceOptions.First(o => o.Name == name);
            bool selected = !_selectedServices.Contains(name);

            if (selected) _selectedServices.Add(name);
            else _selectedServices.Remove(name);

            chip.BackColor = selected ? Theme.SelectionTint : FieldFill;
            chip.HoverColor = selected ? Theme.SelectionTint : Color.FromArgb(240, 241, 245);
            chip.ForeColor = selected ? Theme.Accent : Theme.TextPrimary;
            chip.Font = new Font("Segoe UI", 8.75F, selected ? FontStyle.Bold : FontStyle.Regular);
            chip.Text = selected ? $"✓  {name}\n{option.Price:$0.00}" : $"{name}\n{option.Price:$0.00}";
            chip.Invalidate();

            decimal total = EstimatedCostValue;
            lblSelectedCount.Text = $"{_selectedServices.Count} item{(_selectedServices.Count == 1 ? "" : "s")} selected";
            lblTotalValue.Text = total.ToString("$0.00");
            lblTotalValue.Location = new Point(lblTotalValue.Parent!.Width - lblTotalValue.PreferredWidth - 20, 16);
        }

        // ----- Terms & Agreement -----
        private RoundedPanel BuildTermsCard(int width)
        {
            var card = NewCard(width);
            AddSectionTitle(card, "📄", "Terms & Agreement");

            var termsBox = new Panel
            {
                Location = new Point(24, 76),
                Size = new Size(width - 48, 170),
                BackColor = FieldFill,
                AutoScroll = true
            };
            card.Controls.Add(termsBox);

            termsBox.Controls.Add(new Label
            {
                Text =
                    "By submitting this service request, the customer agrees to the following terms:\n\n" +
                    "1. The shop will provide a diagnostic assessment of the device.\n" +
                    "2. Repair estimates are subject to change upon further inspection.\n" +
                    "3. Parts ordered on the customer's behalf are non-refundable once sourced.\n" +
                    "4. The shop is not liable for pre-existing damage or data loss during repair.\n" +
                    "5. Devices not collected within 30 days of the \"Repaired\" notification may be subject to storage fees.\n\n" +
                    "24-HOUR CANCELLATION POLICY\n" +
                    "Once this service request is submitted, it CANNOT be cancelled or refunded after 24 hours have passed. " +
                    "Cancellations within the first 24 hours must be made in writing or by phone with the shop directly.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Theme.TextPrimary,
                Location = new Point(16, 12),
                Size = new Size(width - 48 - 32 - SystemInformation.VerticalScrollBarWidth, 280),
                AutoSize = false
            });

            int checkY = 76 + 170 + 20;
            chkAcceptTerms = new CheckBox
            {
                Text = "I have read and accept the Terms & Conditions above, including the 24-hour cancellation policy.",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = false,
                Size = new Size(width - 48, 40),
                Location = new Point(24, checkY)
            };
            chkAcceptTerms.CheckedChanged += (s, e) =>
            {
                btnSubmit.Enabled = chkAcceptTerms.Checked;
                btnSubmit.BackColor = chkAcceptTerms.Checked ? Theme.Accent : DisabledGray;
                btnSubmit.HoverColor = chkAcceptTerms.Checked ? Theme.AccentHover : DisabledGrayHover;
                btnSubmit.Invalidate();
            };
            card.Controls.Add(chkAcceptTerms);

            lblError = new Label
            {
                ForeColor = Color.FromArgb(200, 70, 70),
                Font = new Font("Segoe UI", 8.5F),
                AutoSize = false,
                Size = new Size(width - 48, 20),
                Location = new Point(24, checkY + 40),
                Visible = false
            };
            card.Controls.Add(lblError);

            card.Height = checkY + 40 + 20 + 20;
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
