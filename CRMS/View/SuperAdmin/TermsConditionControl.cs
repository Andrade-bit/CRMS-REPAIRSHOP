using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CRMS.winforms.Api;
using CRMS.winforms.Controllers;
using CRMS.winforms.Shared;

namespace CRMS.winforms.SuperAdmin
{
    /// <summary>
    /// Superadmin's Terms &amp; Agreement page: every published version and which
    /// one is current, plus publishing a new version (which becomes current
    /// immediately — there is no separate "roll back to an older version" action).
    /// </summary>
    public partial class TermsConditionControl : UserControl
    {
        private readonly SuperAdminController _controller;

        private DataGridView termsGrid = null!;
        private Label toastLabel = null!;
        private System.Windows.Forms.Timer toastTimer = null!;
        private Button addButton = null!;

        public TermsConditionControl(SuperAdminController controller)
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
            termsGrid.Columns.Add("Plan", "Plan");
            termsGrid.Columns.Add("Created", "Published");
            termsGrid.Columns.Add("Preview", "Preview");
            termsGrid.Columns.Add("Current", "Current");

            var editColumn = new DataGridViewButtonColumn
            {
                Name = "Edit",
                HeaderText = "",
                Text = "",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat
            };
            termsGrid.Columns.Add(editColumn);

            termsGrid.Columns["Version"].FillWeight = 90;
            termsGrid.Columns["Plan"].FillWeight = 90;
            termsGrid.Columns["Created"].FillWeight = 110;
            termsGrid.Columns["Preview"].FillWeight = 260;
            termsGrid.Columns["Current"].FillWeight = 90;
            termsGrid.Columns["Edit"].FillWeight = 55;

            foreach (DataGridViewColumn column in termsGrid.Columns)
            {
                if (column.Name == "Edit") continue;
                column.ReadOnly = true;
            }

            termsGrid.CellClick += TermsGrid_CellClick;
            termsGrid.CellPainting += TermsGrid_CellPainting;

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
                "Terms & Agreement", "Version history for the terms shown to every tenant", addButton, showTitle: false, showSubtitle: false));

            ResumeLayout(false);
        }

        private void ShowEmptyState()
        {
            termsGrid.Rows.Add("No versions published yet.", "", "", "", "", "");
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
                    version.PlanScope == "All" ? "All plans" : version.PlanScope,
                    version.CreatedAt.ToString("MMM d, yyyy"),
                    preview,
                    version.IsCurrent ? "✓ Current" : "",
                    "");

                termsGrid.Rows[rowIndex].Tag = version;

                if (version.IsCurrent)
                {
                    termsGrid.Rows[rowIndex].DefaultCellStyle.ForeColor = Theme.StatusRepairedText;
                }
            }
        }

        private void TermsGrid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (termsGrid.Columns[e.ColumnIndex].Name != "Edit") return;

            e.PaintBackground(e.CellBounds, true);
            IconResources.DrawCentered(e.Graphics!, "edit", e.CellBounds);
            e.Handled = true;
        }

        private async void TermsGrid_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (termsGrid.Columns[e.ColumnIndex].Name != "Edit") return;

            var row = termsGrid.Rows[e.RowIndex];
            if (row.Tag is not TermsVersionEntryDto version) return;

            using var dialog = new EditTermsScopeDialog(version.Version, version.PlanScope);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
            if (dialog.PlanScopeValue == version.PlanScope) return;

            var (success, message) = await _controller.UpdateTermsVersionScopeAsync(version.TermsId, dialog.PlanScopeValue);
            if (!success)
            {
                MessageBox.Show(FindForm(), $"Could not update which plan this version applies to:\n{message}",
                    "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            await LoadData();
            ShowToast($"✓ Version \"{version.Version}\" now applies to {(dialog.PlanScopeValue == "All" ? "All plans" : dialog.PlanScopeValue)}.");
        }

        private async void AddButton_Click(object? sender, EventArgs e)
        {
            var existingVersions = (await _controller.GetTermsVersionsAsync()).Select(v => v.Version);
            using var dialog = new AddTermsVersionDialog(existingVersions);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

            addButton.Enabled = false;
            try
            {
                var (success, message) = await _controller.PublishTermsVersionAsync(dialog.VersionValue, dialog.ContentValue, dialog.PlanScopeValue);
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
