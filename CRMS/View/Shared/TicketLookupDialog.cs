using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CRMS.winforms; // reuses RoundedButton from Login.cs
using CRMS.winforms.Api;
using CRMS.winforms.Controllers;

namespace CRMS.winforms.Shared
{
    /// <summary>
    /// Small read-only popup for the header's "Find ticket #" search — shows just
    /// enough to answer a customer calling in asking about their ticket, without
    /// Staff paging through the full history. Shows a "not found" message when
    /// the number doesn't match any (non-archived) ticket in this tenant.
    /// </summary>
    public class TicketLookupDialog : Form
    {
        private static readonly Dictionary<string, (Color Bg, Color Fg)> StatusPillColors = new()
        {
            [TicketStatus.Diagnose] = (Theme.StatusDiagnoseBg, Theme.StatusDiagnoseText),
            [TicketStatus.AwaitingApproval] = (Theme.StatusAwaitingApprovalBg, Theme.StatusAwaitingApprovalText),
            [TicketStatus.Repairing] = (Theme.StatusRepairingBg, Theme.StatusRepairingText),
            [TicketStatus.Repaired] = (Theme.StatusRepairedBg, Theme.StatusRepairedText),
            [TicketStatus.Cancelled] = (Theme.StatusCancelledBg, Theme.StatusCancelledText),
            [TicketStatus.Unrepairable] = (Theme.StatusUnrepairableBg, Theme.StatusUnrepairableText)
        };

        public TicketLookupDialog(int searchedId, TicketLookupDto? result)
        {
            BuildLayout(searchedId, result);
        }

        private void BuildLayout(int searchedId, TicketLookupDto? result)
        {
            Text = "Ticket Lookup";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(380, result != null ? 260 : 180);
            BackColor = Theme.PageBg;
            Font = new Font("Segoe UI", 9F);

            var title = new Label
            {
                Text = "Ticket Lookup",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 20)
            };
            Controls.Add(title);

            if (result is null)
            {
                var notFound = new Label
                {
                    Text = $"No ticket found with number #{searchedId:0000}.\nIt may have been archived, or the number may be off.",
                    Font = new Font("Segoe UI", 9.5F),
                    ForeColor = Theme.TextSecondary,
                    AutoSize = false,
                    Size = new Size(ClientSize.Width - 48, 60),
                    Location = new Point(24, 56)
                };
                Controls.Add(notFound);
            }
            else
            {
                int y = 58;

                AddCaption("TICKET #", 24, y);
                Controls.Add(new Label
                {
                    Text = $"#{result.TicketId:0000}",
                    Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                    ForeColor = Theme.TextPrimary,
                    AutoSize = true,
                    Location = new Point(24, y + 16)
                });

                var (bg, fg) = StatusPillColors.TryGetValue(result.Status, out var colors) ? colors : (Theme.StatusDiagnoseBg, Theme.StatusDiagnoseText);
                var pill = new Label
                {
                    Text = FriendlyStatus(result.Status),
                    Font = new Font("Segoe UI", 8.75F, FontStyle.Bold),
                    BackColor = bg,
                    ForeColor = fg,
                    AutoSize = true,
                    Padding = new Padding(10, 4, 10, 4),
                    Location = new Point(ClientSize.Width - 24 - 120, y + 12)
                };
                Controls.Add(pill);
                pill.Left = ClientSize.Width - 24 - pill.PreferredWidth;
                Theme.ApplyRoundedRegion(pill, pill.Height / 2);
                y += 56;

                AddCaption("CUSTOMER", 24, y);
                Controls.Add(new Label
                {
                    Text = result.CustomerName,
                    Font = new Font("Segoe UI", 10.5F),
                    ForeColor = Theme.TextPrimary,
                    AutoSize = true,
                    Location = new Point(24, y + 16)
                });
                y += 48;

                AddCaption("DEVICE", 24, y);
                Controls.Add(new Label
                {
                    Text = result.DeviceName,
                    Font = new Font("Segoe UI", 10.5F),
                    ForeColor = Theme.TextPrimary,
                    AutoSize = true,
                    Location = new Point(24, y + 16)
                });
                y += 48;

                AddCaption("RECEIVED", 24, y);
                Controls.Add(new Label
                {
                    Text = result.CreatedAt.ToString("MMM d, yyyy h:mm tt"),
                    Font = new Font("Segoe UI", 10.5F),
                    ForeColor = Theme.TextPrimary,
                    AutoSize = true,
                    Location = new Point(24, y + 16)
                });
            }

            var btnClose = new RoundedButton
            {
                Text = "Close",
                Location = new Point(24, ClientSize.Height - 58),
                Size = new Size(ClientSize.Width - 48, 40),
                BackColor = Theme.Accent,
                HoverColor = Theme.AccentHover,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            btnClose.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            Controls.Add(btnClose);
            AcceptButton = btnClose;
            CancelButton = btnClose;
        }

        private static string FriendlyStatus(string status) => status switch
        {
            TicketStatus.AwaitingApproval => "Awaiting Approval",
            _ => status
        };

        private void AddCaption(string text, int x, int y)
        {
            Controls.Add(new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(x, y)
            });
        }
    }
}
