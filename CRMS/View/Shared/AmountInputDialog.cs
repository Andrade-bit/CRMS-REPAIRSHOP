using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using CRMS.winforms; // reuses RoundedPanel/RoundedButton/RoundedTextBox from Login.cs

namespace CRMS.winforms.Shared
{
    /// <summary>
    /// Small popup that asks for an amount, used when Staff marks a ticket
    /// "Partial" payment — the amount must be more than ₱0 and less than the
    /// ticket's estimated cost (₱0 or the full amount should be Unpaid/Paid
    /// instead, so AmountPaid can never silently disagree with PaymentStatus).
    /// </summary>
    public class AmountInputDialog : Form
    {
        private static readonly Color CardColor = Color.White;
        private static readonly Color CardBorder = Color.FromArgb(232, 234, 238);
        private static readonly Color TitleColor = Color.FromArgb(33, 37, 41);
        private static readonly Color SubtleGray = Color.FromArgb(140, 144, 153);
        private static readonly Color Accent = Color.FromArgb(58, 150, 150);
        private static readonly Color AccentHover = Color.FromArgb(47, 130, 130);
        private static readonly Color PageBg = Color.FromArgb(237, 239, 242);

        private RoundedTextBox txtAmount = null!;
        private Label lblError = null!;
        private readonly decimal _estimatedCost;

        public decimal AmountValue { get; private set; }

        public AmountInputDialog(decimal estimatedCost, decimal? initialAmount = null)
        {
            _estimatedCost = estimatedCost;
            BuildLayout(initialAmount);
        }

        private void BuildLayout(decimal? initialAmount)
        {
            Text = "Record Partial Payment";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(400, 260);
            BackColor = PageBg;
            Font = new Font("Segoe UI", 9F);

            var card = new RoundedPanel
            {
                Location = new Point(16, 16),
                Size = new Size(368, 190),
                BackColor = CardColor,
                CornerRadius = 14,
                BorderColor = CardBorder
            };
            Controls.Add(card);

            card.Controls.Add(new Label
            {
                Text = "How much has the customer paid?",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = TitleColor,
                AutoSize = true,
                Location = new Point(20, 16)
            });

            card.Controls.Add(new Label
            {
                Text = $"Estimated cost: {_estimatedCost:₱#,##0.00}",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(20, 44)
            });

            txtAmount = new RoundedTextBox
            {
                Location = new Point(20, 74),
                Size = new Size(328, 42),
                Placeholder = "0.00",
                TextSize = 12F,
                Value = initialAmount.HasValue && initialAmount.Value > 0
                    ? initialAmount.Value.ToString("0.00", CultureInfo.InvariantCulture)
                    : ""
            };
            card.Controls.Add(txtAmount);

            lblError = new Label
            {
                ForeColor = Color.FromArgb(200, 70, 70),
                Font = new Font("Segoe UI", 9F),
                AutoSize = false,
                Size = new Size(328, 20),
                Location = new Point(20, 124),
                Visible = false
            };
            card.Controls.Add(lblError);

            var btnCancel = new RoundedButton
            {
                Text = "Cancel",
                Location = new Point(16, 216),
                Size = new Size(120, 38),
                CornerRadius = 8,
                BackColor = Color.White,
                HoverColor = Color.FromArgb(245, 246, 248),
                ForeColor = TitleColor,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(btnCancel);

            var btnSave = new RoundedButton
            {
                Text = "Save",
                Location = new Point(148, 216),
                Size = new Size(236, 38),
                CornerRadius = 8,
                BackColor = Accent,
                HoverColor = AccentHover,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);
            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (!decimal.TryParse(txtAmount.Value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            {
                ShowError("Enter a valid amount.");
                return;
            }

            if (amount <= 0 || amount >= _estimatedCost)
            {
                ShowError($"Enter an amount greater than ₱0 and less than {_estimatedCost:₱#,##0.00}.");
                return;
            }

            AmountValue = amount;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visible = true;
        }
    }
}
