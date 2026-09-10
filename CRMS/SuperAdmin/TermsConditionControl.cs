using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS.winforms.Controllers;
using CRMS.winforms.Shared;

namespace CRMS.winforms.SuperAdmin
{
    /// <summary>
    /// Superadmin's Terms &amp; Conditions page: every published version, which
    /// one is current, and a way to publish a new one or roll back to an older one.
    /// </summary>
    public partial class TermsConditionControl : UserControl
    {
        private readonly SuperAdminController _controller;

        private DataGridView termsGrid = null!;
        private Label toastLabel = null!;
        private System.Windows.Forms.Timer toastTimer = null!;

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

            var addButton = new Button
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
            termsGrid.ReadOnly = false; // needs to stay false so the "Set Current" button is clickable
            termsGrid.RowTemplate.Height = 52;

            termsGrid.Columns.Add("Version", "Version");
            termsGrid.Columns.Add("Created", "Published");
            termsGrid.Columns.Add("Preview", "Preview");
            termsGrid.Columns.Add("Current", "Current");

            var setCurrentColumn = new DataGridViewButtonColumn
            {
                Name = "SetCurrent",
                HeaderText = "",
                Text = "Set as Current",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat
            };
            termsGrid.Columns.Add(setCurrentColumn);

            termsGrid.Columns["Version"].FillWeight = 90;
            termsGrid.Columns["Created"].FillWeight = 110;
            termsGrid.Columns["Preview"].FillWeight = 260;
            termsGrid.Columns["Current"].FillWeight = 80;
            termsGrid.Columns["SetCurrent"].FillWeight = 140;

            foreach (DataGridViewColumn column in termsGrid.Columns)
            {
                if (column.Name == "SetCurrent") continue;
                column.ReadOnly = true;
            }

            termsGrid.CellClick += TermsGrid_CellClick;

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
                "Terms & Conditions", "Manage the versioned terms shown to customers", addButton));

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

        /// <summary>Reloads the grid from the controller's terms versions. Call on every page navigation.</summary>
        public void LoadData()
        {
            termsGrid.Rows.Clear();

            var versions = _controller.GetTermsVersions();

            if (versions.Count == 0)
            {
                ShowEmptyState();
                return;
            }

            foreach (var version in versions)
            {
                string preview = version.Content.Length > 80 ? version.Content[..80] + "…" : version.Content;

                int rowIndex = termsGrid.Rows.Add(
                    version.Version,
                    version.CreatedAt.ToString("MMM d, yyyy"),
                    preview,
                    version.IsCurrent ? "✓ Current" : "");

                termsGrid.Rows[rowIndex].Tag = version.TermsId;
                if (version.IsCurrent)
                {
                    termsGrid.Rows[rowIndex].DefaultCellStyle.ForeColor = Theme.StatusRepairedText;
                }
            }
        }

        private void TermsGrid_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (termsGrid.Columns[e.ColumnIndex].Name != "SetCurrent") return;

            var row = termsGrid.Rows[e.RowIndex];
            if (row.Tag is not int termsId) return;

            _controller.SetCurrentTermsVersion(termsId);
            LoadData();
            ShowToast("✓ Current Terms & Conditions version updated.");
        }

        private void AddButton_Click(object? sender, EventArgs e)
        {
            using var dialog = new AddTermsVersionDialog();
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

            _controller.PublishTermsVersion(dialog.VersionValue, dialog.ContentValue);

            LoadData();
            ShowToast($"✓ Version \"{dialog.VersionValue}\" published and set as current.");
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
