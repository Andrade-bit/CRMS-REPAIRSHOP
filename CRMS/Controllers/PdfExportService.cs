using System;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CRMS.winforms.Controllers
{
    /// <summary>Generates a one-page PDF receipt for a completed service ticket.</summary>
    public static class PdfExportService
    {
        static PdfExportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        /// <summary>Builds the receipt and writes it to <paramref name="filePath"/>.</summary>
        public static void ExportTicketHistory(TicketRecord ticket, string filePath)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Computer Repair Shop").FontSize(18).Bold();
                        col.Item().Text("Service Ticket Receipt").FontSize(11).FontColor(Colors.Grey.Darken1);
                        col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().PaddingTop(12).Column(col =>
                    {
                        col.Spacing(6);

                        col.Item().Text($"Ticket #: {ticket.TicketId}").Bold();
                        col.Item().Text($"Customer: {ticket.FullName}");
                        col.Item().Text($"Phone: {ticket.PhoneNumber}");
                        col.Item().Text($"Email: {ticket.Email}");

                        col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten3);

                        col.Item().PaddingTop(8).Text($"Device: {ticket.DeviceName}");
                        col.Item().Text($"Concern: {ticket.Concern}");
                        col.Item().Text($"Status: {ticket.Status}");
                        col.Item().Text($"Date Received: {ticket.CreatedAt:MMM d, yyyy h:mm tt}");
                        col.Item().Text($"Date Completed: {ticket.CompletedAt?.ToString("MMM d, yyyy h:mm tt") ?? "-"}");

                        col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten3);

                        col.Item().PaddingTop(8).Text($"Handled By: {(string.IsNullOrWhiteSpace(ticket.StaffName) ? "-" : ticket.StaffName)}").Bold();

                        col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten3);

                        col.Item().PaddingTop(8).Text($"Estimated Cost: {ticket.EstimatedCost:C}").Bold().FontSize(13);
                    });

                    page.Footer().AlignCenter().Text(
                        $"Generated {DateTime.Now:MMM d, yyyy h:mm tt}").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            }).GeneratePdf(filePath);
        }
    }
}
