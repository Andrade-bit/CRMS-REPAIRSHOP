using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS.winforms; // reuses RoundedPanel/RoundedButton/RoundedTextBox from Login.cs
using CRMS.winforms.Shared; // reuses RoundedMultilineBox

namespace CRMS.winforms.StaffView
{
    /// <summary>
    /// Popup for recording a ticket's diagnosis findings and, once the customer
    /// has responded, their approval decision. This is the one real gate before
    /// paid repair work can begin — Repairing is blocked server-side until a
    /// ticket's ApprovalStatus is "Approved" (see StaffTicketsController.UpdateStatus).
    /// </summary>
    public class RecordApprovalDialog : Form
    {
        private static readonly Color CardColor = Color.White;
        private static readonly Color CardBorder = Color.FromArgb(232, 234, 238);
        private static readonly Color TitleColor = Color.FromArgb(33, 37, 41);
        private static readonly Color SubtleGray = Color.FromArgb(140, 144, 153);
        private static readonly Color Accent = Color.FromArgb(58, 150, 150);
        private static readonly Color AccentHover = Color.FromArgb(47, 130, 130);
        private static readonly Color RejectColor = Color.FromArgb(190, 70, 70);
        private static readonly Color RejectHover = Color.FromArgb(165, 55, 55);
        private static readonly Color PageBg = Color.FromArgb(237, 239, 242);
        private static readonly Color FieldFill = Color.FromArgb(249, 249, 251);

        private RoundedMultilineBox txtFindings = null!;
        private RoundedTextBox txtEstimatedCost = null!;
        private RadioButton radPending = null!;
        private RadioButton radApproved = null!;
        private RadioButton radRejected = null!;
        private RoundedTextBox txtApprovedAmount = null!;
        private ComboBox cmbMethod = null!;
        private Label lblApprovedAmountCaption = null!;
        private Label lblMethodCaption = null!;
        private RoundedButton btnSubmit = null!;
        private Label lblError = null!;

        public string FindingsValue => txtFindings.Value.Trim();
        public decimal EstimatedCostValue => decimal.TryParse(txtEstimatedCost.Value, out var v) ? v : 0m;
        public string DecisionValue =>
            radApproved.Checked ? "Approved" : radRejected.Checked ? "Rejected" : "Pending";
        public decimal? ApprovedAmountValue =>
            decimal.TryParse(txtApprovedAmount.Value, out var v) ? v : (decimal?)null;
        public string? MethodValue => cmbMethod.SelectedItem?.ToString();

        public RecordApprovalDialog(string? existingFindings, decimal currentEstimatedCost, string currentApprovalStatus)
        {
            BuildLayout(existingFindings, currentEstimatedCost, currentApprovalStatus);
        }

        private void BuildLayout(string? existingFindings, decimal currentEstimatedCost, string currentApprovalStatus)
        {
            Text = "Diagnosis & Customer Approval";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, 566);
            BackColor = PageBg;
            Font = new Font("Segoe UI", 9F);

            var card = new RoundedPanel
            {
                Location = new Point(16, 16),
                Size = new Size(488, 534),
                BackColor = CardColor,
                CornerRadius = 14,
                BorderColor = CardBorder
            };
            Controls.Add(card);

            card.Controls.Add(new Label
            {
                Text = "Diagnosis & Customer Approval",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = TitleColor,
                AutoSize = true,
                Location = new Point(20, 18)
            });

            card.Controls.Add(new Label
            {
                Text = "DIAGNOSIS FINDINGS",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(20, 56)
            });
            txtFindings = new RoundedMultilineBox
            {
                Location = new Point(20, 78),
                Size = new Size(448, 80),
                Placeholder = "What did the technician find? (e.g. faulty RAM module, liquid damage on motherboard)",
                TextSize = 10F
            };
            if (!string.IsNullOrWhiteSpace(existingFindings))
                txtFindings.Value = existingFindings;
            card.Controls.Add(txtFindings);

            card.Controls.Add(new Label
            {
                Text = "PROPOSED SERVICE / ESTIMATED COST",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(20, 170)
            });
            txtEstimatedCost = new RoundedTextBox
            {
                Location = new Point(20, 192),
                Size = new Size(200, 40),
                Placeholder = "0.00",
                TextSize = 11F,
                Value = currentEstimatedCost > 0 ? currentEstimatedCost.ToString("0.00") : ""
            };
            card.Controls.Add(txtEstimatedCost);

            card.Controls.Add(new Label
            {
                Text = "CUSTOMER'S DECISION",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(20, 250)
            });

            radPending = new RadioButton
            {
                Text = "Still deciding — just save findings",
                Font = new Font("Segoe UI", 10F),
                ForeColor = TitleColor,
                AutoSize = true,
                Location = new Point(20, 274),
                Checked = string.IsNullOrEmpty(currentApprovalStatus) || currentApprovalStatus is "NotRequired" or "Pending"
            };
            radApproved = new RadioButton
            {
                Text = "Approved — customer agreed, start repair",
                Font = new Font("Segoe UI", 10F),
                ForeColor = TitleColor,
                AutoSize = true,
                Location = new Point(20, 300),
                Checked = currentApprovalStatus == "Approved"
            };
            radRejected = new RadioButton
            {
                Text = "Rejected — customer declined, cancel ticket",
                Font = new Font("Segoe UI", 10F),
                ForeColor = TitleColor,
                AutoSize = true,
                Location = new Point(20, 326),
                Checked = currentApprovalStatus == "Rejected"
            };
            card.Controls.Add(radPending);
            card.Controls.Add(radApproved);
            card.Controls.Add(radRejected);

            lblApprovedAmountCaption = new Label
            {
                Text = "APPROVED AMOUNT",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(20, 360)
            };
            txtApprovedAmount = new RoundedTextBox
            {
                Location = new Point(20, 382),
                Size = new Size(200, 40),
                Placeholder = "0.00",
                TextSize = 11F
            };
            card.Controls.Add(lblApprovedAmountCaption);
            card.Controls.Add(txtApprovedAmount);

            lblMethodCaption = new Label
            {
                Text = "HOW WAS THIS COMMUNICATED?",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = SubtleGray,
                AutoSize = true,
                Location = new Point(240, 360)
            };
            cmbMethod = new ComboBox
            {
                Location = new Point(240, 382),
                Size = new Size(228, 40),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            cmbMethod.Items.AddRange(new object[] { "In-person", "Phone", "SMS", "Email" });
            cmbMethod.SelectedIndex = 0;
            card.Controls.Add(lblMethodCaption);
            card.Controls.Add(cmbMethod);

            void SyncDecisionFields()
            {
                bool needsDecisionFields = radApproved.Checked || radRejected.Checked;
                lblApprovedAmountCaption.Visible = radApproved.Checked;
                txtApprovedAmount.Visible = radApproved.Checked;
                lblMethodCaption.Visible = needsDecisionFields;
                cmbMethod.Visible = needsDecisionFields;

                if (radApproved.Checked && string.IsNullOrWhiteSpace(txtApprovedAmount.Value))
                    txtApprovedAmount.Value = txtEstimatedCost.Value;

                btnSubmit.Text = radApproved.Checked ? "Confirm Approval — Move to Repairing"
                    : radRejected.Checked ? "Confirm Rejection — Cancel Ticket"
                    : "Save Findings";
                btnSubmit.BackColor = radRejected.Checked ? RejectColor : Accent;
                btnSubmit.HoverColor = radRejected.Checked ? RejectHover : AccentHover;
                btnSubmit.Invalidate();
            }
            radPending.CheckedChanged += (s, e) => SyncDecisionFields();
            radApproved.CheckedChanged += (s, e) => SyncDecisionFields();
            radRejected.CheckedChanged += (s, e) => SyncDecisionFields();

            lblError = new Label
            {
                ForeColor = Color.FromArgb(200, 70, 70),
                Font = new Font("Segoe UI", 9.5F),
                AutoSize = false,
                Size = new Size(448, 20),
                Location = new Point(20, 432),
                Visible = false
            };
            card.Controls.Add(lblError);

            var btnCancel = new RoundedButton
            {
                Text = "Cancel",
                Location = new Point(20, 460),
                Size = new Size(120, 42),
                CornerRadius = 0,
                BackColor = Color.White,
                HoverColor = FieldFill,
                ForeColor = TitleColor,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold)
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            card.Controls.Add(btnCancel);

            btnSubmit = new RoundedButton
            {
                Text = "Save Findings",
                Location = new Point(148, 460),
                Size = new Size(320, 42),
                CornerRadius = 0,
                BackColor = Accent,
                HoverColor = AccentHover,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold)
            };
            btnSubmit.Click += BtnSubmit_Click;
            card.Controls.Add(btnSubmit);

            SyncDecisionFields();
        }

        private void BtnSubmit_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FindingsValue))
            {
                lblError.Text = "Please record what the diagnosis found before saving.";
                lblError.Visible = true;
                return;
            }

            if (EstimatedCostValue <= 0)
            {
                lblError.Text = "Please enter a proposed cost greater than zero.";
                lblError.Visible = true;
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
