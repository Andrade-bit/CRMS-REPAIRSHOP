using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS.winforms; // reuses RoundedPanel/RoundedButton from Login.cs

namespace CRMS.winforms.Shared
{
    /// <summary>
    /// Small popup for recording a customer's feedback on a completed ticket.
    /// Customers don't have their own login in this app, so whoever hands the
    /// device back (Staff, usually) enters this on the customer's behalf — a
    /// required 1-5 star rating plus an optional comment. Reused both as a
    /// prompt right after marking a ticket picked up, and as an "edit later"
    /// popup from Customer History.
    /// </summary>
    public class FeedbackDialog : Form
    {
        private static readonly Color CardColor = Color.White;
        private static readonly Color CardBorder = Color.FromArgb(232, 234, 238);
        private static readonly Color TitleColor = Color.FromArgb(33, 37, 41);
        private static readonly Color SubtleGray = Color.FromArgb(140, 144, 153);
        private static readonly Color Accent = Color.FromArgb(58, 150, 150);
        private static readonly Color AccentHover = Color.FromArgb(47, 130, 130);
        private static readonly Color PageBg = Color.FromArgb(237, 239, 242);
        private static readonly Color StarOn = Color.FromArgb(250, 176, 5);
        private static readonly Color StarOff = Color.FromArgb(214, 218, 224);

        private readonly Label[] _stars = new Label[5];
        private readonly string _customerName;
        private RoundedMultilineBox txtMessage = null!;
        private Label lblRatingCaption = null!;
        private Label lblError = null!;

        public int RatingValue { get; private set; }
        public string MessageValue { get; private set; } = "";

        public FeedbackDialog(string customerName, int? initialRating = null, string? initialMessage = null)
        {
            _customerName = customerName;
            RatingValue = initialRating ?? 0;
            BuildLayout(initialMessage);
        }

        private void BuildLayout(string? initialMessage)
        {
            Text = "Customer Feedback";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(420, 340);
            BackColor = PageBg;
            Font = new Font("Segoe UI", 9F);

            var card = new RoundedPanel
            {
                Location = new Point(16, 16),
                Size = new Size(388, 270),
                BackColor = CardColor,
                CornerRadius = 14,
                BorderColor = CardBorder
            };
            Controls.Add(card);

            card.Controls.Add(new Label
            {
                Text = $"How was the service for {_customerName}?",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = TitleColor,
                AutoSize = true,
                MaximumSize = new Size(348, 0),
                Location = new Point(20, 16)
            });

            lblRatingCaption = new Label
            {
                Text = "RATING",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(20, 60)
            };
            card.Controls.Add(lblRatingCaption);

            for (int i = 0; i < _stars.Length; i++)
            {
                int starValue = i + 1;
                var star = new Label
                {
                    Text = "★",
                    Font = new Font("Segoe UI", 26F),
                    AutoSize = true,
                    Location = new Point(20 + i * 44, 78),
                    Cursor = Cursors.Hand
                };
                star.Click += (s, e) => SetRating(starValue);
                _stars[i] = star;
                card.Controls.Add(star);
            }
            SetRating(RatingValue);

            card.Controls.Add(new Label
            {
                Text = "COMMENT (OPTIONAL)",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(20, 142)
            });

            txtMessage = new RoundedMultilineBox
            {
                Location = new Point(20, 162),
                Size = new Size(348, 70),
                Placeholder = "What did the customer say? (optional)",
                TextSize = 10F,
                Value = initialMessage ?? ""
            };
            card.Controls.Add(txtMessage);

            lblError = new Label
            {
                ForeColor = Color.FromArgb(200, 70, 70),
                Font = new Font("Segoe UI", 9F),
                AutoSize = false,
                Size = new Size(348, 20),
                Location = new Point(20, 238),
                Visible = false
            };
            card.Controls.Add(lblError);

            var btnSkip = new RoundedButton
            {
                Text = "Skip",
                Location = new Point(16, 296),
                Size = new Size(120, 38),
                CornerRadius = 8,
                BackColor = Color.White,
                HoverColor = Color.FromArgb(245, 246, 248),
                ForeColor = TitleColor,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnSkip.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(btnSkip);

            var btnSave = new RoundedButton
            {
                Text = "Save Feedback",
                Location = new Point(148, 296),
                Size = new Size(256, 38),
                CornerRadius = 8,
                BackColor = Accent,
                HoverColor = AccentHover,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);
            AcceptButton = btnSave;
            CancelButton = btnSkip;
        }

        private void SetRating(int rating)
        {
            RatingValue = rating;
            for (int i = 0; i < _stars.Length; i++)
                _stars[i].ForeColor = i < rating ? StarOn : StarOff;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (RatingValue is < 1 or > 5)
            {
                lblError.Text = "Tap a star to rate the service.";
                lblError.Visible = true;
                return;
            }

            MessageValue = txtMessage.Value.Trim();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
