using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS.winforms; // reuses RoundedPanel/RoundedButton/RoundedTextBox from Login.cs

namespace CRMS.winforms.Shared
{
    /// <summary>
    /// Popup used by Admin's User Management page to create a Staff or Manager
    /// account. Collects the fields, validates them, then leaves it to the
    /// caller to persist via AdminController.CreateAccount(...).
    /// </summary>
    public class CreateAccountDialog : Form
    {
        private RoundedTextBox txtFullName = null!;
        private RoundedTextBox txtUsername = null!;
        private RoundedTextBox txtPassword = null!;
        private ComboBox cboRole = null!;
        private Label lblError = null!;
        private RoundedButton btnCreate = null!;

        public string FullNameValue => txtFullName.Value.Trim();
        public string UsernameValue => txtUsername.Value.Trim();
        public string PasswordValue => txtPassword.Value;
        public string RoleValue => cboRole.SelectedItem?.ToString() ?? "Staff";

        public CreateAccountDialog()
        {
            BuildLayout();
        }

        private void BuildLayout()
        {
            Text = "Create Account";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(380, 430);
            BackColor = Theme.PageBg;
            Font = new Font("Segoe UI", 9F);

            var title = new Label
            {
                Text = "Create Staff / Manager Account",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 20)
            };
            Controls.Add(title);

            int y = 66;
            AddCaption("FULL NAME", y);
            txtFullName = AddTextBox("e.g. Juan Dela Cruz", y + 20);
            y += 66;

            AddCaption("USERNAME", y);
            txtUsername = AddTextBox("e.g. jdelacruz", y + 20);
            y += 66;

            AddCaption("PASSWORD", y);
            txtPassword = AddTextBox("Temporary password", y + 20, isPassword: true);
            y += 66;

            AddCaption("ROLE", y);
            cboRole = new ComboBox
            {
                Location = new Point(24, y + 20),
                Size = new Size(ClientSize.Width - 48, 32),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            cboRole.Items.AddRange(new object[] { "Staff", "Manager" });
            cboRole.SelectedIndex = 0;
            Controls.Add(cboRole);
            y += 56;

            lblError = new Label
            {
                ForeColor = Color.FromArgb(200, 70, 70),
                Font = new Font("Segoe UI", 8.5F),
                AutoSize = false,
                Size = new Size(ClientSize.Width - 48, 32),
                Location = new Point(24, y),
                Visible = false
            };
            Controls.Add(lblError);
            y += 32;

            btnCreate = new RoundedButton
            {
                Text = "Create Account",
                Location = new Point(24, y),
                Size = new Size(ClientSize.Width - 48, 42),
                BackColor = Theme.Accent,
                HoverColor = Theme.AccentHover,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnCreate.Click += BtnCreate_Click;
            Controls.Add(btnCreate);
            AcceptButton = btnCreate;
        }

        private void AddCaption(string text, int y)
        {
            Controls.Add(new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, y)
            });
        }

        private RoundedTextBox AddTextBox(string placeholder, int y, bool isPassword = false)
        {
            var box = new RoundedTextBox
            {
                Location = new Point(24, y),
                Size = new Size(ClientSize.Width - 48, 36),
                Placeholder = placeholder,
                IsPassword = isPassword
            };
            Controls.Add(box);
            return box;
        }

        private void BtnCreate_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FullNameValue) ||
                string.IsNullOrWhiteSpace(UsernameValue) ||
                string.IsNullOrWhiteSpace(PasswordValue))
            {
                lblError.Text = "Please fill in all fields.";
                lblError.Visible = true;
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
