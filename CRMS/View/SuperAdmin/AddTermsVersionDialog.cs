using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CRMS.winforms; // reuses RoundedButton/RoundedTextBox from Login.cs
using CRMS.winforms.Shared;

namespace CRMS.winforms.SuperAdmin
{
    /// <summary>Popup for publishing a new Terms &amp; Conditions version.</summary>
    public class AddTermsVersionDialog : Form
    {
        private const int MaxVersionLength = 16;

        private readonly HashSet<string> _existingVersions;

        private RoundedTextBox txtVersion = null!;
        private ComboBox cmbPlanScope = null!;
        private RoundedMultilineBox txtContent = null!;
        private Label lblError = null!;
        private RoundedButton btnPublish = null!;

        public string VersionValue => txtVersion.Value.Trim();
        public string ContentValue => txtContent.Value.Trim();

        /// <summary>"All" or one of the three plan names — which plan this
        /// version applies to. "All" is a fallback used wherever no
        /// plan-specific current version exists for that plan.</summary>
        public string PlanScopeValue => cmbPlanScope.SelectedItem?.ToString() switch
        {
            "Starter" => "Starter",
            "Growth" => "Growth",
            "AllAccess" => "AllAccess",
            _ => "All"
        };

        /// <param name="existingVersions">Every version label already published —
        /// checked so a new one can't collide with one already in use.</param>
        public AddTermsVersionDialog(IEnumerable<string>? existingVersions = null)
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
            ClientSize = new Size(520, 580);
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
                Text = "APPLIES TO",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, 122)
            });
            cmbPlanScope = new ComboBox
            {
                Location = new Point(24, 140),
                Size = new Size(ClientSize.Width - 48, 36),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            cmbPlanScope.Items.AddRange(new object[] { "All plans", "Starter", "Growth", "AllAccess" });
            cmbPlanScope.SelectedIndex = 0;
            Controls.Add(cmbPlanScope);

            Controls.Add(new Label
            {
                Text = "CONTENT",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, 186)
            });
            txtContent = new RoundedMultilineBox
            {
                Location = new Point(24, 204),
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
                Location = new Point(24, 492),
                Visible = false
            };
            Controls.Add(lblError);

            btnPublish = new RoundedButton
            {
                Text = "Publish Version",
                Location = new Point(24, 520),
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
