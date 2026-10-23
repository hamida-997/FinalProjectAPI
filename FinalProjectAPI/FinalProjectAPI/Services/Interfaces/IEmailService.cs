namespace FinalProjectAPI.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody);
        Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string htmlBody);
    }
}
