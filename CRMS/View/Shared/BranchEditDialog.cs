using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS.winforms; // reuses RoundedPanel/RoundedButton/RoundedTextBox from Login.cs
using CRMS.winforms.Api;

namespace CRMS.winforms.Shared
{
    /// <summary>
    /// Popup used by Admin's Branch Management page to add a new branch or edit
    /// an existing one — same dialog either way, just prefilled when editing.
    /// Collects the fields, validates them, then leaves it to the caller to
    /// persist via AdminController.CreateBranchAsync/UpdateBranchAsync.
    /// </summary>
    public class BranchEditDialog : Form
    {
        private RoundedTextBox txtName = null!;
        private RoundedTextBox txtAddress = null!;
        private RoundedTextBox txtPhone = null!;
        private RoundedTextBox txtEmail = null!;
        private Label lblError = null!;
        private RoundedButton btnSave = null!;

        public string NameValue => txtName.Value.Trim();
        public string AddressValue => txtAddress.Value.Trim();
        public string PhoneValue => txtPhone.Value.Trim();
        public string EmailValue => txtEmail.Value.Trim();

        public BranchEditDialog(BranchDto? existing = null)
        {
            BuildLayout(existing);
        }

        private void BuildLayout(BranchDto? existing)
        {
            bool isEdit = existing != null;

            Text = isEdit ? "Edit Branch" : "Add Branch";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(440, 400);
            BackColor = Theme.PageBg;
            Font = new Font("Segoe UI", 9F);

            var title = new Label
            {
                Text = isEdit ? "Edit Branch" : "Add Branch",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 20)
            };
            Controls.Add(title);
            var subtitle = new Label
            {
                Text = isEdit ? "Update this branch's details" : "This tenant can have more than one branch",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, 46)
            };
            Controls.Add(subtitle);

            int contentWidth = ClientSize.Width - 48;
            int y = 80;

            AddCaption("BRANCH NAME", 24, y);
            txtName = AddTextBox("e.g. QuickFix Computer Repair — Downtown", 24, y + 18, contentWidth);
            txtName.Value = existing?.Name ?? "";
            y += 66;

            AddCaption("ADDRESS", 24, y);
            txtAddress = AddTextBox("Street, City, Province", 24, y + 18, contentWidth);
            txtAddress.Value = existing?.Address ?? "";
            y += 66;

            int gap = 12;
            int halfWidth = (contentWidth - gap) / 2;

            AddCaption("PHONE", 24, y);
            txtPhone = AddTextBox("09XX XXX XXXX", 24, y + 18, halfWidth, inputMode: TextInputMode.Phone);
            txtPhone.Value = existing?.Phone ?? "";

            AddCaption("EMAIL", 24 + halfWidth + gap, y);
            txtEmail = AddTextBox("branch@shop.com", 24 + halfWidth + gap, y + 18, contentWidth - halfWidth - gap);
            txtEmail.Value = existing?.Email ?? "";
            y += 66;

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

            btnSave = new RoundedButton
            {
                Text = isEdit ? "Save Changes" : "Add Branch",
                Location = new Point(24, y),
                Size = new Size(contentWidth, 42),
                BackColor = Theme.Accent,
                HoverColor = Theme.AccentHover,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);
            AcceptButton = btnSave;
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

        private RoundedTextBox AddTextBox(string placeholder, int x, int y, int width, TextInputMode inputMode = TextInputMode.Any)
        {
            var box = new RoundedTextBox
            {
                Location = new Point(x, y),
                Size = new Size(width, 36),
                Placeholder = placeholder,
                InputMode = inputMode
            };
            Controls.Add(box);
            return box;
        }

        private static readonly System.Text.RegularExpressions.Regex PhonePattern = new(@"^[0-9][0-9\s\-+()]*$");
        private static readonly System.Text.RegularExpressions.Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameValue))
            {
                lblError.Text = "Please enter a branch name.";
                lblError.Visible = true;
                return;
            }

            if (PhoneValue.Length > 0 && !PhonePattern.IsMatch(PhoneValue))
            {
                lblError.Text = "Phone number can only contain digits — no letters.";
                lblError.Visible = true;
                return;
            }

            if (EmailValue.Length > 0 && !EmailPattern.IsMatch(EmailValue))
            {
                lblError.Text = "Enter a valid email address, e.g. branch@shop.com.";
                lblError.Visible = true;
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
