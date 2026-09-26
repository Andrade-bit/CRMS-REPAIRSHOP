using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CRMS.winforms.Controllers;
using CRMS.winforms.Shared;

namespace CRMS.Winforms.Admin
{
    /// <summary>
    /// Admin's own Terms &amp; Conditions page: this tenant's version history and
    /// publishing a new one. Separate from Superadmin's platform-wide Terms &amp;
    /// Agreement — this is the text shown to customers on the Add Customer
    /// screen, and publishing a new version here is what changes that.
    /// </summary>
    public partial class TermsConditionControl : UserControl
    {
        private readonly AdminController _controller;

        private DataGridView termsGrid = null!;
        private Label toastLabel = null!;
        private System.Windows.Forms.Timer toastTimer = null!;
        private Button addButton = null!;

        public TermsConditionControl(AdminController controller)
        {
            _controller = controller;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            BackColor = Theme.PageBg;
            Padding = new Padding(Theme.PageMarginX, Theme.PageMarginTop, Theme.PageMarginX, Theme.PageMarginTop);

            addButton = new Button
            {
                Text = "＋  New Version",
                Width = 150,
                Height = 38,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Accent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            addButton.FlatAppearance.BorderSize = 0;
            addButton.FlatAppearance.MouseOverBackColor = Theme.AccentHover;
            addButton.FlatAppearance.MouseDownBackColor = Theme.AccentPressed;
            Theme.ApplyRoundedRegion(addButton, 8);
            addButton.Click += AddButton_Click;

            var tableCard = Theme.CreateTableCard();

            termsGrid = Theme.CreateStyledGrid();
            termsGrid.RowTemplate.Height = 52;

            termsGrid.Columns.Add("Version", "Version");
            termsGrid.Columns.Add("Created", "Published");
            termsGrid.Columns.Add("Preview", "Preview");
            termsGrid.Columns.Add("Current", "Status");

            termsGrid.Columns["Version"].FillWeight = 90;
            termsGrid.Columns["Created"].FillWeight = 110;
            termsGrid.Columns["Preview"].FillWeight = 320;
            termsGrid.Columns["Current"].FillWeight = 90;

            foreach (DataGridViewColumn column in termsGrid.Columns)
                column.ReadOnly = true;

            ShowEmptyState();

            tableCard.Controls.Add(termsGrid);

            toastLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 0,
                BackColor = Theme.StatusRepairedBg,
                ForeColor = Theme.StatusRepairedText,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };
            toastTimer = new System.Windows.Forms.Timer { Interval = 3000 };
            toastTimer.Tick += (s, e) => { toastTimer.Stop(); HideToast(); };

            Controls.Add(tableCard);
            Controls.Add(toastLabel);
            Controls.Add(Theme.CreateSpacer());
            Controls.Add(Theme.CreatePageHeader(
                "Terms & Conditions", "Version history for the terms shown to your customers", addButton, showTitle: false, showSubtitle: false));

            ResumeLayout(false);
        }

        private void ShowEmptyState()
        {
            termsGrid.Rows.Add("No versions published yet.", "", "", "");
            termsGrid.Rows[0].ReadOnly = true;
            termsGrid.Rows[0].DefaultCellStyle = new DataGridViewCellStyle
            {
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 9.75F, FontStyle.Italic)
            };
        }

        /// <summary>Reloads the grid from the real API. Call on every page navigation.</summary>
        public async System.Threading.Tasks.Task LoadData()
        {
            termsGrid.Rows.Clear();

            var versions = await _controller.GetTermsVersionsAsync();

            if (versions.Count == 0)
            {
                ShowEmptyState();
                return;
            }

            foreach (var version in versions)
            {
                string preview = version.Content.Length > 90 ? version.Content[..90] + "…" : version.Content;

                int rowIndex = termsGrid.Rows.Add(
                    version.Version,
                    version.CreatedAt.ToString("MMM d, yyyy"),
                    preview,
                    version.IsCurrent ? "✓ Current" : "");

                if (version.IsCurrent)
                {
                    termsGrid.Rows[rowIndex].DefaultCellStyle.ForeColor = Theme.StatusRepairedText;
                }
            }
        }

        private async void AddButton_Click(object? sender, EventArgs e)
        {
            var existingVersions = (await _controller.GetTermsVersionsAsync()).Select(v => v.Version);
            using var dialog = new AddTenantTermsVersionDialog(existingVersions);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

            addButton.Enabled = false;
            try
            {
                var (success, message) = await _controller.PublishTermsVersionAsync(dialog.VersionValue, dialog.ContentValue);
                if (!success)
                {
                    MessageBox.Show(FindForm(), $"Could not publish this version:\n{message}",
                        "Publish Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                await LoadData();
                ShowToast($"✓ Version \"{dialog.VersionValue}\" published and set as current.");
            }
            finally
            {
                addButton.Enabled = true;
            }
        }

        private void ShowToast(string message)
        {
            toastLabel.Text = message;
            toastLabel.Height = 36;
            toastLabel.Visible = true;
            toastTimer.Stop();
            toastTimer.Start();
        }

        private void HideToast()
        {
            toastLabel.Visible = false;
            toastLabel.Height = 0;
        }
    }
}
