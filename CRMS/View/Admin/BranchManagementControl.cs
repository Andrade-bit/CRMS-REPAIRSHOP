using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS.winforms.Api;
using CRMS.winforms.Controllers;
using CRMS.winforms.Shared;

namespace CRMS.Winforms.Admin
{
    /// <summary>
    /// Admin's Branch Management page — every branch this tenant has (name,
    /// address, phone, email), with Add/Edit. Only ever shown at all when this
    /// tenant's database actually has the Branches table; see AdminMainForm's
    /// startup check.
    /// </summary>
    public partial class BranchManagementControl : UserControl
    {
        private readonly AdminController _controller;

        private DataGridView branchGrid = null!;
        private Panel tableCard = null!;
        private Button addBranchBtn = null!;

        public BranchManagementControl(AdminController controller)
        {
            _controller = controller;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            BackColor = Theme.PageBg;
            Padding = new Padding(Theme.PageMarginX, Theme.PageMarginTop, Theme.PageMarginX, Theme.PageMarginTop);

            addBranchBtn = new Button
            {
                Text = "＋  Add Branch",
                Width = 150,
                Height = 38,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Accent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            addBranchBtn.FlatAppearance.BorderSize = 0;
            addBranchBtn.FlatAppearance.MouseOverBackColor = Theme.AccentHover;
            addBranchBtn.FlatAppearance.MouseDownBackColor = Theme.AccentPressed;
            Theme.ApplyRoundedRegion(addBranchBtn, 8);
            addBranchBtn.Click += AddBranchBtn_Click;

            tableCard = Theme.CreateTableCard();

            branchGrid = Theme.CreateStyledGrid();
            branchGrid.RowTemplate.Height = 52;

            branchGrid.Columns.Add("Name", "Branch Name");
            branchGrid.Columns.Add("Address", "Address");
            branchGrid.Columns.Add("Phone", "Phone");
            branchGrid.Columns.Add("Email", "Email");

            var editColumn = new DataGridViewButtonColumn
            {
                Name = "Edit",
                HeaderText = "",
                Text = "✎ Edit",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat
            };
            branchGrid.Columns.Add(editColumn);

            branchGrid.Columns["Name"].FillWeight = 160;
            branchGrid.Columns["Address"].FillWeight = 220;
            branchGrid.Columns["Phone"].FillWeight = 120;
            branchGrid.Columns["Email"].FillWeight = 160;
            branchGrid.Columns["Edit"].FillWeight = 90;

            foreach (DataGridViewColumn column in branchGrid.Columns)
            {
                if (column.Name == "Edit") continue;
                column.ReadOnly = true;
            }

            branchGrid.CellClick += BranchGrid_CellClick;

            ShowEmptyState();

            tableCard.Controls.Add(branchGrid);

            Controls.Add(tableCard);
            Controls.Add(Theme.CreateSpacer());
            Controls.Add(Theme.CreatePageHeader(
                "Branch Management", "Add and manage this shop's branches", addBranchBtn, showTitle: false, showSubtitle: false));

            ResumeLayout(false);
        }

        private void ShowEmptyState()
        {
            branchGrid.Rows.Add("No branches yet.", "", "", "", "");
            branchGrid.Rows[0].ReadOnly = true;
            branchGrid.Rows[0].DefaultCellStyle = new DataGridViewCellStyle
            {
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 9.75F, FontStyle.Italic),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0)
            };
        }

        /// <summary>Reloads the grid with this tenant's real branches. Call on every page navigation.</summary>
        public async Task LoadData()
        {
            branchGrid.CellClick -= BranchGrid_CellClick;
            branchGrid.Rows.Clear();

            var branches = await _controller.GetBranchesAsync();

            if (branches is null || branches.Count == 0)
            {
                ShowEmptyState();
            }
            else
            {
                foreach (var branch in branches)
                {
                    int rowIndex = branchGrid.Rows.Add(branch.Name, branch.Address, branch.Phone, branch.Email);
                    branchGrid.Rows[rowIndex].Tag = branch;
                }
            }

            branchGrid.CellClick += BranchGrid_CellClick;
        }

        private async void AddBranchBtn_Click(object? sender, EventArgs e)
        {
            using var dialog = new BranchEditDialog();
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

            addBranchBtn.Enabled = false;
            try
            {
                var (success, message, _) = await _controller.CreateBranchAsync(
                    dialog.NameValue, dialog.AddressValue, dialog.PhoneValue, dialog.EmailValue);

                if (!success)
                {
                    MessageBox.Show(FindForm(), $"Could not add the branch:\n{message}",
                        "Add Branch Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                await LoadData();
            }
            finally
            {
                addBranchBtn.Enabled = true;
            }
        }

        private async void BranchGrid_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (branchGrid.Columns[e.ColumnIndex].Name != "Edit") return;

            var row = branchGrid.Rows[e.RowIndex];
            if (row.Tag is not BranchDto branch) return;

            using var dialog = new BranchEditDialog(branch);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

            var (success, message) = await _controller.UpdateBranchAsync(
                branch.BranchId, dialog.NameValue, dialog.AddressValue, dialog.PhoneValue, dialog.EmailValue);

            if (!success)
            {
                MessageBox.Show(FindForm(), $"Could not save the branch details:\n{message}",
                    "Save Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            await LoadData();
        }
    }
}
