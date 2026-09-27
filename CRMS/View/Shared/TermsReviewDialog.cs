using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS.winforms; // reuses RoundedButton from Login.cs

namespace CRMS.winforms.Shared
{
    /// <summary>
    /// Gate shown before a subscription is actually saved — displays the
    /// terms version that applies to the plan being assigned and requires the
    /// "I have read and agree" checkbox before Confirm is enabled. Cancelling
    /// (or closing) here aborts the whole subscription save; nothing is
    /// submitted to the API until this dialog returns OK.
    /// </summary>
    public class TermsReviewDialog : Form
    {
        private CheckBox chkAgree = null!;
        private RoundedButton btnConfirm = null!;

        public TermsReviewDialog(string planDisplayName, string termsVersion, string termsContent)
        {
            BuildLayout(planDisplayName, termsVersion, termsContent);
        }

        private void BuildLayout(string planDisplayName, string termsVersion, string termsContent)
        {
            Text = "Review Terms & Agreement";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, 560);
            BackColor = Theme.PageBg;
            Font = new Font("Segoe UI", 9F);

            int contentWidth = ClientSize.Width - 48;

            var title = new Label
            {
                Text = $"Terms for the {planDisplayName} plan",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 20)
            };
            Controls.Add(title);

            var subtitle = new Label
            {
                Text = $"Version {termsVersion} — read before confirming this subscription",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, 46)
            };
            Controls.Add(subtitle);

            var contentBox = new TextBox
            {
                Location = new Point(24, 78),
                Size = new Size(contentWidth, 380),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5F),
                Text = termsContent,
                BackColor = Color.White
            };
            Controls.Add(contentBox);

            chkAgree = new CheckBox
            {
                Text = "I have read and agree to these terms",
                Location = new Point(24, 470),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Theme.TextPrimary
            };
            chkAgree.CheckedChanged += (s, e) => btnConfirm.Enabled = chkAgree.Checked;
            Controls.Add(chkAgree);

            var btnCancel = new RoundedButton
            {
                Text = "Cancel",
                Location = new Point(24, 506),
                Size = new Size((contentWidth - 12) / 2, 42),
                BackColor = Color.White,
                HoverColor = Color.FromArgb(245, 246, 248),
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(btnCancel);

            btnConfirm = new RoundedButton
            {
                Text = "Confirm Subscription",
                Location = new Point(24 + (contentWidth - 12) / 2 + 12, 506),
                Size = new Size((contentWidth - 12) / 2, 42),
                BackColor = Theme.Accent,
                HoverColor = Theme.AccentHover,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Enabled = false
            };
            btnConfirm.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            Controls.Add(btnConfirm);
            CancelButton = btnCancel;
        }
    }
}
