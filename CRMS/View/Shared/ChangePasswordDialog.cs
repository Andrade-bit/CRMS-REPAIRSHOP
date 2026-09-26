using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS.winforms; // reuses RoundedButton/RoundedTextBox from Login.cs

namespace CRMS.winforms.Shared
{
    /// <summary>Popup for setting a new password on an existing Staff/Manager account.</summary>
    public class ChangePasswordDialog : Form
    {
        private RoundedTextBox txtNewPassword = null!;
        private Label lblError = null!;
        private RoundedButton btnSave = null!;

        public string NewPasswordValue => txtNewPassword.Value;

        public ChangePasswordDialog(string username, string fullName)
        {
            BuildLayout(username, fullName);
        }

        private void BuildLayout(string username, string fullName)
        {
            Text = "Change Password";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(360, 260);
            BackColor = Theme.PageBg;
            Font = new Font("Segoe UI", 9F);

            var title = new Label
            {
                Text = "Change Password",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 20)
            };
            Controls.Add(title);

            var subtitle = new Label
            {
                Text = $"{fullName} ({username})",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, 46)
            };
            Controls.Add(subtitle);

            Controls.Add(new Label
            {
                Text = "NEW PASSWORD",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, 82)
            });
            txtNewPassword = new RoundedTextBox
            {
                Location = new Point(24, 100),
                Size = new Size(ClientSize.Width - 48, 36),
                Placeholder = "Enter new password",
                IsPassword = true
            };
            Controls.Add(txtNewPassword);

            lblError = new Label
            {
                ForeColor = Color.FromArgb(200, 70, 70),
                Font = new Font("Segoe UI", 8.5F),
                AutoSize = false,
                Size = new Size(ClientSize.Width - 48, 28),
                Location = new Point(24, 144),
                Visible = false
            };
            Controls.Add(lblError);

            btnSave = new RoundedButton
            {
                Text = "Save New Password",
                Location = new Point(24, 180),
                Size = new Size(ClientSize.Width - 48, 42),
                BackColor = Theme.Accent,
                HoverColor = Theme.AccentHover,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);
            AcceptButton = btnSave;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewPasswordValue))
            {
                lblError.Text = "Please enter a new password.";
                lblError.Visible = true;
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
