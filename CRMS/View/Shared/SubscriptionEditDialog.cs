using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CRMS.winforms; // reuses RoundedPanel/RoundedButton/RoundedTextBox from Login.cs
using CRMS.winforms.Api;

namespace CRMS.winforms.Shared
{
    /// <summary>
    /// Popup used by Superadmin's Subscriptions page to set an already-onboarded
    /// tenant's billing cycle, next billing date, and subscription amount all
    /// together. Only ever picks among tenants that already exist — Superadmin
    /// never creates a new tenant here (that's still a hardcoded/manual step),
    /// this only edits an existing one's billing details.
    /// </summary>
    public class SubscriptionEditDialog : Form
    {
        private readonly List<TenantSubscriptionEntryDto> _tenants;

        private ComboBox cboTenant = null!;
        private ComboBox cboBillingCycle = null!;
        private Label lblPlanStatus = null!;
        private DateTimePicker dtpNextBilling = null!;
        private CheckBox chkNoNextBilling = null!;
        private RoundedTextBox txtAmount = null!;
        private Label lblError = null!;
        private RoundedButton btnSave = null!;

        private string _planKey = "AllAccess";

        public int CompanyIdValue => _tenants[cboTenant.SelectedIndex].CompanyId;
        public string BillingCycleValue => string.IsNullOrWhiteSpace(cboBillingCycle.Text) ? "Monthly" : cboBillingCycle.Text.Trim();

        /// <summary>Which module set to apply — set via Browse Plans, or carried
        /// over from the selected tenant's current plan if Browse Plans was
        /// never opened.</summary>
        public string PlanKeyValue => _planKey;
        public DateTime? NextBillingDateValue => chkNoNextBilling.Checked ? null : dtpNextBilling.Value.Date;
        public decimal? AmountValue
        {
            get
            {
                string raw = txtAmount.Value.Trim().TrimStart('₱').Replace(",", "");
                return decimal.TryParse(raw, out var parsed) ? parsed : null;
            }
        }

        /// <summary>tenants: every currently-onboarded tenant to choose from — the
        /// grid's own already-loaded list, so no extra API call is needed here.</summary>
        public SubscriptionEditDialog(List<TenantSubscriptionEntryDto> tenants)
        {
            _tenants = tenants;
            BuildLayout();
        }

        private void BuildLayout()
        {
            Text = "Subscribe";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(440, 484);
            BackColor = Theme.PageBg;
            Font = new Font("Segoe UI", 9F);

            var title = new Label
            {
                Text = "Manage Subscription",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 20)
            };
            Controls.Add(title);
            var subtitle = new Label
            {
                Text = "Set billing cycle, next billing date, and amount for an existing tenant",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, 46)
            };
            Controls.Add(subtitle);

            int contentWidth = ClientSize.Width - 48;
            int y = 84;

            AddCaption("TENANT", 24, y);
            cboTenant = new ComboBox
            {
                Location = new Point(24, y + 18),
                Size = new Size(contentWidth, 32),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            foreach (var tenant in _tenants)
                cboTenant.Items.Add($"{tenant.CompanyName} — {tenant.AdminName}");
            Controls.Add(cboTenant);
            y += 66;

            AddCaption("BILLING CYCLE", 24, y);
            int billingComboWidth = contentWidth - 132;
            cboBillingCycle = new ComboBox
            {
                Location = new Point(24, y + 18),
                Size = new Size(billingComboWidth, 32),
                // Editable (not DropDownList) so a custom cycle can be typed —
                // Monthly/Yearly are just the two quick picks in the dropdown.
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                Font = new Font("Segoe UI", 9.5F)
            };
            cboBillingCycle.Items.AddRange(new object[] { "Monthly", "Yearly" });
            Controls.Add(cboBillingCycle);

            var btnBrowsePlans = new RoundedButton
            {
                Text = "📋 Browse Plans",
                Location = new Point(24 + billingComboWidth + 12, y + 17),
                Size = new Size(contentWidth - billingComboWidth - 12, 34),
                CornerRadius = 8,
                BackColor = Color.White,
                HoverColor = Color.FromArgb(245, 246, 248),
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };
            btnBrowsePlans.Click += BtnBrowsePlans_Click;
            Controls.Add(btnBrowsePlans);
            y += 40;

            lblPlanStatus = new Label
            {
                Text = "Plan: All Access",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Theme.Accent,
                AutoSize = true,
                Location = new Point(24, y)
            };
            Controls.Add(lblPlanStatus);
            y += 26;

            AddCaption("NEXT BILLING DATE", 24, y);
            dtpNextBilling = new DateTimePicker
            {
                Location = new Point(24, y + 18),
                Size = new Size(contentWidth, 28),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 9.5F)
            };
            Controls.Add(dtpNextBilling);
            y += 52;

            chkNoNextBilling = new CheckBox
            {
                Text = "No next billing date set",
                Location = new Point(24, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Theme.TextSecondary
            };
            chkNoNextBilling.CheckedChanged += (s, e) => dtpNextBilling.Enabled = !chkNoNextBilling.Checked;
            Controls.Add(chkNoNextBilling);
            y += 34;

            AddCaption("AMOUNT (PER CYCLE)", 24, y);
            txtAmount = new RoundedTextBox
            {
                Location = new Point(24, y + 18),
                Size = new Size(contentWidth, 36),
                Placeholder = "e.g. 1500.00"
            };
            Controls.Add(txtAmount);
            y += 66;

            cboTenant.SelectedIndexChanged += (s, e) => PrefillFromSelectedTenant();
            if (cboTenant.Items.Count > 0) cboTenant.SelectedIndex = 0;

            lblError = new Label
            {
                ForeColor = Color.FromArgb(200, 70, 70),
                Font = new Font("Segoe UI", 8.5F),
                AutoSize = false,
                Size = new Size(contentWidth, 24),
                Location = new Point(24, y),
                Visible = false
            };
            Controls.Add(lblError);
            y += 24;

            btnSave = new RoundedButton
            {
                Text = "Save Subscription",
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

        /// <summary>Loads the selected tenant's current billing cycle/next billing
        /// date/amount into the fields, so editing starts from its real values
        /// instead of blank defaults.</summary>
        private void PrefillFromSelectedTenant()
        {
            if (cboTenant.SelectedIndex < 0) return;
            var tenant = _tenants[cboTenant.SelectedIndex];

            cboBillingCycle.Text = tenant.BillingCycle;

            _planKey = tenant.Plan;
            lblPlanStatus.Text = "Plan: " + PlanDisplayName(_planKey);

            if (tenant.NextBillingDate.HasValue)
            {
                chkNoNextBilling.Checked = false;
                dtpNextBilling.Value = tenant.NextBillingDate.Value.Date;
            }
            else
            {
                chkNoNextBilling.Checked = true;
            }

            txtAmount.Value = tenant.Amount.HasValue ? tenant.Amount.Value.ToString("0.00") : "";
        }

        /// <summary>Opens the bigger plan-picker popup; picking a plan there fills
        /// in Billing Cycle and Amount here — nothing is saved until this
        /// dialog's own Save Subscription button is pressed.</summary>
        private void BtnBrowsePlans_Click(object? sender, EventArgs e)
        {
            using var picker = new SubscriptionPlanPickerDialog();
            if (picker.ShowDialog(this) != DialogResult.OK) return;

            cboBillingCycle.Text = picker.SelectedBillingCycle;
            txtAmount.Value = picker.SelectedAmount.ToString("0.00");
            _planKey = picker.SelectedPlanKey;
            lblPlanStatus.Text = "Plan: " + PlanDisplayName(_planKey);
        }

        private static string PlanDisplayName(string key) => key switch
        {
            "Growth" => "Growth",
            "Starter" => "Starter",
            _ => "All Access"
        };

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

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (cboTenant.SelectedIndex < 0)
            {
                lblError.Text = "Select a tenant.";
                lblError.Visible = true;
                return;
            }

            string rawAmount = txtAmount.Value.Trim();
            if (!string.IsNullOrEmpty(rawAmount) && AmountValue is null)
            {
                lblError.Text = "Enter a valid amount, or leave it blank.";
                lblError.Visible = true;
                return;
            }
            if (AmountValue is < 0)
            {
                lblError.Text = "Amount can't be negative.";
                lblError.Visible = true;
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
