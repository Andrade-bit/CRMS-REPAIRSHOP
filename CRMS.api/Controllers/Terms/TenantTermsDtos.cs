namespace CRMS.api.Controllers
{
    public record TenantTermsVersionEntry(int TermsId, string Version, string Content, DateTime CreatedAt, bool IsCurrent);
    public record PublishTenantTermsRequest(string Version, string Content);
    public record CurrentTermsResponse(string? Version, string? Content);
}
