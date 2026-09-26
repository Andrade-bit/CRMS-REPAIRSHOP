using System.Windows.Forms;
using CRMS.winforms.Shared;

namespace CRMS.Winforms.Admin
{
    public partial class MembershipControl : UserControl
    {
        public MembershipControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            BackColor = Theme.PageBg;
            Padding = new Padding(Theme.PageMarginX, Theme.PageMarginTop, Theme.PageMarginX, Theme.PageMarginTop);

            Controls.Add(Theme.CreatePlaceholderCard("Membership content is coming soon."));
            Controls.Add(Theme.CreateSpacer());
            Controls.Add(Theme.CreatePageHeader("Membership", "Manage membership plans and subscribers", showTitle: false, showSubtitle: false));

            ResumeLayout(false);
        }
    }
}
