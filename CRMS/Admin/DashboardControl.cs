using System.Windows.Forms;

namespace CRMS.Winforms.Admin
{
    public partial class DashboardControl : UserControl
    {
        public DashboardControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            BackColor = Theme.PageBg;
            Padding = new Padding(Theme.PageMarginX, Theme.PageMarginTop, Theme.PageMarginX, Theme.PageMarginTop);

            Controls.Add(Theme.CreatePlaceholderCard("Dashboard content is coming soon."));
            Controls.Add(Theme.CreateSpacer());
            Controls.Add(Theme.CreatePageHeader("Dashboard", "A quick overview of your organization"));

            ResumeLayout(false);
        }
    }
}
