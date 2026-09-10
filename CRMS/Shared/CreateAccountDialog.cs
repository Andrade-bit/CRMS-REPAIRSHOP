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
        private RoundedTextBox txtFirstName = null!;
        private RoundedTextBox txtMiddleName = null!;
        private RoundedTextBox txtLastName = null!;
        private RoundedTextBox txtUsername = null!;
        private RoundedTextBox txtPassword = null!;
        private ComboBox cboRole = null!;
        private Label lblError = null!;
        private RoundedButton btnCreate = null!;

        public string FirstNameValue => txtFirstName.Value.Trim();
        public string MiddleNameValue => txtMiddleName.Value.Trim();
        public string LastNameValue => txtLastName.Value.Trim();
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
            ClientSize = new Size(440, 470);
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
            var subtitle = new Label
            {
                Text = "Admin creates login credentials for their staff",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, 46)
            };
            Controls.Add(subtitle);

            int contentWidth = ClientSize.Width - 48;
            int gap = 12;
            int triWidth = (contentWidth - gap * 2) / 3;
            int lastTriWidth = contentWidth - triWidth * 2 - gap * 2; // absorb rounding so the row fills exactly

            int y = 80;
            AddCaption("FIRST NAME", 24, y);
            AddCaption("MIDDLE NAME", 24 + triWidth + gap, y);
            AddCaption("LAST NAME", 24 + (triWidth + gap) * 2, y);
            txtFirstName = AddTextBox("e.g. Juan", 24, y + 18, triWidth);
            txtMiddleName = AddTextBox("e.g. Reyes", 24 + triWidth + gap, y + 18, triWidth);
            txtLastName = AddTextBox("e.g. Dela Cruz", 24 + (triWidth + gap) * 2, y + 18, lastTriWidth);
            y += 66;

            AddCaption("USERNAME", 24, y);
            txtUsername = AddTextBox("e.g. jdelacruz", 24, y + 18, contentWidth);
            y += 66;

            AddCaption("PASSWORD", 24, y);
            txtPassword = AddTextBox("Temporary password", 24, y + 18, contentWidth, isPassword: true);
            y += 66;

            AddCaption("ROLE", 24, y);
            cboRole = new ComboBox
            {
                Location = new Point(24, y + 18),
                Size = new Size(contentWidth, 32),
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
                Size = new Size(contentWidth, 28),
                Location = new Point(24, y),
                Visible = false
            };
            Controls.Add(lblError);
            y += 28;

            btnCreate = new RoundedButton
            {
                Text = "Create Account",
                Location = new Point(24, y),
                Size = new Size(contentWidth, 42),
                BackColor = Theme.Accent,
                HoverColor = Theme.AccentHover,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnCreate.Click += BtnCreate_Click;
            Controls.Add(btnCreate);
            AcceptButton = btnCreate;
        }

        private void AddCaption(string text, int x, int y)
        {
            Controls.Add(new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(x, y)
            });
        }

        private RoundedTextBox AddTextBox(string placeholder, int x, int y, int width, bool isPassword = false)
        {
            var box = new RoundedTextBox
            {
                Location = new Point(x, y),
                Size = new Size(width, 36),
                Placeholder = placeholder,
                IsPassword = isPassword
            };
            Controls.Add(box);
            return box;
        }

        private void BtnCreate_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FirstNameValue) ||
                string.IsNullOrWhiteSpace(LastNameValue) ||
                string.IsNullOrWhiteSpace(UsernameValue) ||
                string.IsNullOrWhiteSpace(PasswordValue))
            {
                lblError.Text = "Please fill in first name, last name, username, and password.";
                lblError.Visible = true;
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
