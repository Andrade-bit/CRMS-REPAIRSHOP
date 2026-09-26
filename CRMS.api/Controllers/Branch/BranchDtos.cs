namespace CRMS.api.Controllers
{
    public record BranchResponse(int BranchId, string Name, string Address, string Phone, string Email);
    public record CreateBranchRequest(string Name, string Address, string Phone, string Email);
    public record UpdateBranchRequest(string Name, string Address, string Phone, string Email);
}
