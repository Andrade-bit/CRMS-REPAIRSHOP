using System.Windows.Forms;

namespace CRMS.Winforms.Admin
{
    public partial class AnalyticReportControl : UserControl
    {
        public AnalyticReportControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            BackColor = Theme.PageBg;
            Padding = new Padding(Theme.PageMarginX, Theme.PageMarginTop, Theme.PageMarginX, Theme.PageMarginTop);

            Controls.Add(Theme.CreatePlaceholderCard("Reporting content is coming soon."));
            Controls.Add(Theme.CreateSpacer());
            Controls.Add(Theme.CreatePageHeader("Reports", "Analytics and reporting tools"));

            ResumeLayout(false);
        }
    }
}
