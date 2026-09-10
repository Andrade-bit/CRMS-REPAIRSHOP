using System;
using System.Drawing;
using System.Windows.Forms;

namespace CRMS.winforms.StaffView
{
    public partial class ReportsCustomer : UserControl
    {
        // =========================================================
        // COLORS (matches NewServiceRequestForm's teal theme)
        // =========================================================
        private static readonly Color PageBg = Color.White;
        private static readonly Color TextPrimary = Color.FromArgb(33, 37, 41);
        private static readonly Color TextSecondary = Color.FromArgb(140, 144, 153);
        private static readonly Color BorderLight = Color.FromArgb(232, 234, 238);
        private static readonly Color HeaderBg = Color.FromArgb(250, 250, 252);
        private static readonly Color RowAlt = Color.FromArgb(248, 249, 251);
        private static readonly Color SelectionBg = Color.FromArgb(230, 244, 244);

        private DataGridView reportsGrid;
        private Panel tableCard;

        public ReportsCustomer()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.BackColor = PageBg;
            this.Padding = new Padding(0);

            // ============================================
            // TITLE
            // ============================================
            Panel topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = PageBg
            };

            Label title = new Label
            {
                Text = "Customer Reports",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = TextPrimary,
                Location = new Point(0, 4),
                AutoSize = true
            };

            Label subtitle = new Label
            {
                Text = "Overview of all service requests and their status",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = TextSecondary,
                Location = new Point(0, 28),
                AutoSize = true
            };

            topPanel.Controls.Add(title);
            topPanel.Controls.Add(subtitle);

            Panel spacer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 16,
                BackColor = PageBg
            };

            // ============================================
            // TABLE CARD
            // ============================================
            tableCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1)
            };
            tableCard.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(BorderLight, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, tableCard.Width - 1, tableCard.Height - 1);
                }
            };

            reportsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = BorderLight,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = false,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 46 },
                ColumnHeadersHeight = 42,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                EnableHeadersVisualStyles = false,
                Font = new Font("Segoe UI", 9.25F),
                ForeColor = TextPrimary
            };

            reportsGrid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = HeaderBg,
                ForeColor = TextSecondary,
                Font = new Font("Segoe UI", 8.75F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 16, 0)
            };

            reportsGrid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9.25F),
                Padding = new Padding(16, 0, 16, 0),
                SelectionBackColor = SelectionBg,
                SelectionForeColor = TextPrimary
            };

            reportsGrid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = RowAlt,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9.25F),
                Padding = new Padding(16, 0, 16, 0),
                SelectionBackColor = SelectionBg,
                SelectionForeColor = TextPrimary
            };

            // ============================================
            // COLUMNS
            // ============================================
            reportsGrid.Columns.Add("CustomerId", "Customer ID");
            reportsGrid.Columns.Add("CustomerName", "Customer Name");
            reportsGrid.Columns.Add("DeviceName", "Device Name");
            reportsGrid.Columns.Add("Status", "Status");
            reportsGrid.Columns.Add("Payment", "Payment");
            reportsGrid.Columns.Add("EstimatedCost", "Estimated Cost");
            reportsGrid.Columns.Add("Concern", "Concern");

            reportsGrid.Columns["CustomerId"].FillWeight = 90;
            reportsGrid.Columns["CustomerName"].FillWeight = 140;
            reportsGrid.Columns["DeviceName"].FillWeight = 150;
            reportsGrid.Columns["Status"].FillWeight = 100;
            reportsGrid.Columns["Payment"].FillWeight = 100;
            reportsGrid.Columns["EstimatedCost"].FillWeight = 110;
            reportsGrid.Columns["Concern"].FillWeight = 200;

            ShowEmptyState();

            tableCard.Controls.Add(reportsGrid);

            this.Controls.Add(tableCard);
            this.Controls.Add(spacer);
            this.Controls.Add(topPanel);

            this.ResumeLayout(false);
        }

        private void ShowEmptyState()
        {
            reportsGrid.Rows.Add("No records found.", "", "", "", "", "", "");
            reportsGrid.Rows[0].DefaultCellStyle = new DataGridViewCellStyle
            {
                ForeColor = TextSecondary,
                Font = new Font("Segoe UI", 9.25F, FontStyle.Italic),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0)
            };
        }

        // =========================================================
        // PUBLIC METHODS
        // =========================================================

        public void AddRecord(string customerId, string customerName, string deviceName,
            string status, string payment, string estimatedCost, string concern)
        {
            if (reportsGrid.Rows.Count == 1 && reportsGrid.Rows[0].Cells[0].Value?.ToString() == "No records found.")
            {
                reportsGrid.Rows.Clear();
            }

            reportsGrid.Rows.Add(customerId, customerName, deviceName, status, payment, estimatedCost, concern);
        }

        public void ClearRecords()
        {
            reportsGrid.Rows.Clear();
            ShowEmptyState();
        }
    }
}