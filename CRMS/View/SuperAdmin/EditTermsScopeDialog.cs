using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS.winforms; // reuses RoundedButton from Login.cs
using CRMS.winforms.Shared;

namespace CRMS.winforms.SuperAdmin
{
    /// <summary>Popup for moving an already-published Terms &amp; Agreement
    /// version to a different plan — changes only which plan it appears
    /// under, never its version label or content.</summary>
    public class EditTermsScopeDialog : Form
    {
        private ComboBox cmbPlanScope = null!;
        private RoundedButton btnSave = null!;

        public string PlanScopeValue => cmbPlanScope.SelectedItem?.ToString() switch
        {
            "Starter" => "Starter",
            "Growth" => "Growth",
            "AllAccess" => "AllAccess",
            _ => "All"
        };

        public EditTermsScopeDialog(string version, string currentPlanScope)
        {
            BuildLayout(version, currentPlanScope);
        }

        private void BuildLayout(string version, string currentPlanScope)
        {
            Text = "Edit Terms Version";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(380, 210);
            BackColor = Theme.PageBg;
            Font = new Font("Segoe UI", 9F);

            int contentWidth = ClientSize.Width - 48;

            var title = new Label
            {
                Text = $"Version {version}",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 20)
            };
            Controls.Add(title);

            var subtitle = new Label
            {
                Text = "Choose which plan this version applies to",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, 46)
            };
            Controls.Add(subtitle);

            Controls.Add(new Label
            {
                Text = "APPLIES TO",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(24, 82)
            });
            cmbPlanScope = new ComboBox
            {
                Location = new Point(24, 100),
                Size = new Size(contentWidth, 36),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            cmbPlanScope.Items.AddRange(new object[] { "All plans", "Starter", "Growth", "AllAccess" });
            cmbPlanScope.SelectedItem = currentPlanScope == "All" ? "All plans" : currentPlanScope;
            if (cmbPlanScope.SelectedIndex < 0) cmbPlanScope.SelectedIndex = 0;
            Controls.Add(cmbPlanScope);

            btnSave = new RoundedButton
            {
                Text = "Save",
                Location = new Point(24, 150),
                Size = new Size(contentWidth, 42),
                BackColor = Theme.Accent,
                HoverColor = Theme.AccentHover,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnSave.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            Controls.Add(btnSave);
            AcceptButton = btnSave;
        }
    }
}
