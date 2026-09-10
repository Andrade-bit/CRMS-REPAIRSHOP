using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS.winforms; // reuses RoundedButton/RoundedTextBox from Login.cs
using CRMS.winforms.Shared;

namespace CRMS.winforms.SuperAdmin
{
    /// <summary>Popup for publishing a new Terms &amp; Conditions version.</summary>
    public class AddTermsVersionDialog : Form
    {
        private RoundedTextBox txtVersion = null!;
        private RoundedMultilineBox txtContent = null!;
        private Label lblError = null!;
        private RoundedButton btnPublish = null!;

        public string VersionValue => txtVersion.Value.Trim();
        public string ContentValue => txtContent.Value.Trim();

        public AddTermsVersionDialog()
        {
            BuildLayout();
        }

        private void BuildLayout()
        {
            Text = "New Terms & Conditions Version";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, 520);
            BackColor = Theme.PageBg;
            Font = new Font("Segoe UI", 9F);

            var title = new Label
            {
                Text = "Publish New Terms Version",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 20)
            };
            Controls.Add(title);

            Controls.Add(new Label
            {
                Text = "VERSION LABEL",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, 58)
            });
            txtVersion = new RoundedTextBox
            {
                Location = new Point(24, 76),
                Size = new Size(ClientSize.Width - 48, 36),
                Placeholder = "e.g. v2.1"
            };
            Controls.Add(txtVersion);

            Controls.Add(new Label
            {
                Text = "CONTENT",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, 126)
            });
            txtContent = new RoundedMultilineBox
            {
                Location = new Point(24, 144),
                Size = new Size(ClientSize.Width - 48, 280),
                Placeholder = "Full terms & conditions text shown to customers..."
            };
            Controls.Add(txtContent);

            lblError = new Label
            {
                ForeColor = Color.FromArgb(200, 70, 70),
                Font = new Font("Segoe UI", 8.5F),
                AutoSize = false,
                Size = new Size(ClientSize.Width - 48, 24),
                Location = new Point(24, 432),
                Visible = false
            };
            Controls.Add(lblError);

            btnPublish = new RoundedButton
            {
                Text = "Publish Version",
                Location = new Point(24, 460),
                Size = new Size(ClientSize.Width - 48, 42),
                BackColor = Theme.Accent,
                HoverColor = Theme.AccentHover,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnPublish.Click += BtnPublish_Click;
            Controls.Add(btnPublish);
            AcceptButton = btnPublish;
        }

        private void BtnPublish_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(VersionValue) || string.IsNullOrWhiteSpace(ContentValue))
            {
                lblError.Text = "Please provide both a version label and the content.";
                lblError.Visible = true;
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
