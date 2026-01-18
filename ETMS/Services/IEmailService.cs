namespace ETMS.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(List<string> recipients, string subject, string body);
    }

}
