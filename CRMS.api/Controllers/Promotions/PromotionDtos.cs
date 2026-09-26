namespace CRMS.api.Controllers
{
    // Shared by PromotionsController, ManagerPromotionsController, and
    // AdminPromotionsController.

    public record CreatePromotionRequest(string Title, string Type, string Message, string Reason, bool SubmitForApproval = true);

    public record PromotionResponse(
        int PromotionId, string Title, string Type, string Message, string Reason, string Status,
        string StaffName, DateTime CreatedAt, DateTime? ReviewedAt);
}
