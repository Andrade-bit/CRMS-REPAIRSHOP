using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS.winforms; // reuses RoundedPanel/RoundedButton/RoundedTextBox from Login.cs
using CRMS.winforms.Shared;

namespace CRMS.winforms.SuperAdmin
{
    /// <summary>
    /// Popup used by Superadmin's Tenants page to onboard a new repair shop:
    /// creates the shop record plus its owning Admin account.
    /// </summary>
    public class AddTenantDialog : Form
    {
        private RoundedTextBox txtShopName = null!;
        private RoundedTextBox txtAdminFullName = null!;
        private RoundedTextBox txtAdminUsername = null!;
        private RoundedTextBox txtAdminPassword = null!;
        private Label lblError = null!;
        private RoundedButton btnCreate = null!;

        public string ShopNameValue => txtShopName.Value.Trim();
        public string AdminFullNameValue => txtAdminFullName.Value.Trim();
        public string AdminUsernameValue => txtAdminUsername.Value.Trim();
        public string AdminPasswordValue => txtAdminPassword.Value;

        public AddTenantDialog()
        {
            BuildLayout();
        }

        private void BuildLayout()
        {
            Text = "Add Tenant";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(380, 470);
            BackColor = Theme.PageBg;
            Font = new Font("Segoe UI", 9F);

            var title = new Label
            {
                Text = "Onboard New Tenant",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 20)
            };
            Controls.Add(title);
            var subtitle = new Label
            {
                Text = "Creates the shop and its Admin login",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, 46)
            };
            Controls.Add(subtitle);

            int y = 82;
            AddCaption("SHOP NAME", y);
            txtShopName = AddTextBox("e.g. QuickFix Computer Repair", y + 20);
            y += 66;

            AddCaption("ADMIN FULL NAME", y);
            txtAdminFullName = AddTextBox("e.g. Maria Santos", y + 20);
            y += 66;

            AddCaption("ADMIN USERNAME", y);
            txtAdminUsername = AddTextBox("e.g. maria.santos", y + 20);
            y += 66;

            AddCaption("ADMIN TEMPORARY PASSWORD", y);
            txtAdminPassword = AddTextBox("Temporary password", y + 20, isPassword: true);
            y += 66;

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
                Text = "Create Tenant",
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
            if (string.IsNullOrWhiteSpace(ShopNameValue) ||
                string.IsNullOrWhiteSpace(AdminFullNameValue) ||
                string.IsNullOrWhiteSpace(AdminUsernameValue) ||
                string.IsNullOrWhiteSpace(AdminPasswordValue))
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
