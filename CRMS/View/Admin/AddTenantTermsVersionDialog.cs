using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CRMS.winforms; // reuses RoundedButton/RoundedTextBox from Login.cs
using CRMS.winforms.Shared;

namespace CRMS.Winforms.Admin
{
    /// <summary>Popup for publishing a new version of this tenant's own Terms &amp; Conditions.</summary>
    public class AddTenantTermsVersionDialog : Form
    {
        private const int MaxVersionLength = 16;

        private readonly HashSet<string> _existingVersions;

        private RoundedTextBox txtVersion = null!;
        private RoundedMultilineBox txtContent = null!;
        private Label lblError = null!;
        private RoundedButton btnPublish = null!;

        public string VersionValue => txtVersion.Value.Trim();
        public string ContentValue => txtContent.Value.Trim();

        /// <param name="existingVersions">Every version label this tenant has already
        /// published — checked so a new one can't collide with one already in use.</param>
        public AddTenantTermsVersionDialog(IEnumerable<string>? existingVersions = null)
        {
            _existingVersions = new HashSet<string>(existingVersions ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
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
            var subtitle = new Label
            {
                Text = "This becomes current immediately — Staff will see it on their next ticket.",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, 46)
            };
            Controls.Add(subtitle);

            Controls.Add(new Label
            {
                Text = "VERSION LABEL",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, 78)
            });
            txtVersion = new RoundedTextBox
            {
                Location = new Point(24, 96),
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
                Location = new Point(24, 146)
            });
            txtContent = new RoundedMultilineBox
            {
                Location = new Point(24, 164),
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
                Location = new Point(24, 452),
                Visible = false
            };
            Controls.Add(lblError);

            btnPublish = new RoundedButton
            {
                Text = "Publish Version",
                Location = new Point(24, 476),
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

            if (VersionValue.Length > MaxVersionLength)
            {
                lblError.Text = $"Use a version label of {MaxVersionLength} characters or fewer.";
                lblError.Visible = true;
                return;
            }

            if (_existingVersions.Contains(VersionValue))
            {
                lblError.Text = "That version already exists. Enter a different version.";
                lblError.Visible = true;
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
