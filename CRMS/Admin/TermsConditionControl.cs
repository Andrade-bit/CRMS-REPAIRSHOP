using System.Windows.Forms;
using CRMS.winforms.Shared;

namespace CRMS.Winforms.Admin
{
    // Note: not currently wired into AdminMainForm's sidebar navigation (no
    // matching nav item) — restyled to match the rest of the app so it's
    // ready to drop in wherever it's meant to be used.
    public partial class TermsConditionControl : UserControl
    {
        public TermsConditionControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            BackColor = Theme.PageBg;
            Padding = new Padding(Theme.PageMarginX, Theme.PageMarginTop, Theme.PageMarginX, Theme.PageMarginTop);

            Controls.Add(Theme.CreatePlaceholderCard("Terms & Conditions editor is coming soon."));
            Controls.Add(Theme.CreateSpacer());
            Controls.Add(Theme.CreatePageHeader("Terms & Conditions", "Manage the terms and conditions shown to users"));

            ResumeLayout(false);
        }
    }
}
