namespace CRMS.infrastructure.Services
{
    /// <summary>Sends a plain-text email to a customer. Implementations must
    /// never throw — a notification failing to send should never break the
    /// ticket operation that triggered it.</summary>
    public interface IEmailService
    {
        Task SendAsync(string toEmail, string toName, string subject, string body);
    }
}
