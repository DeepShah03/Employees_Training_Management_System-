using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace ETMS.Services
{
    public class EmailService : IEmailService
    {
        private readonly SmtpClient _smtpClient;

        public EmailService()
        {
            _smtpClient = new SmtpClient("smtp.gmail.com") 
            {
                Port = 587, // Use the appropriate port (587 for TLS, 465 for SSL)
                Credentials = new NetworkCredential("dkshah2503@gmail.com", "alst adeq bxib txkt"),

                
                EnableSsl = true
            };
        }

        public async Task SendEmailAsync(List<string> recipients, string subject, string body)
        {
            foreach (var recipient in recipients)
            {
                using (var mailMessage = new MailMessage())
                {
                    mailMessage.From = new MailAddress("dkshah2503@gmail.com");
                    mailMessage.Subject = subject;
                    mailMessage.Body = body;
                    mailMessage.IsBodyHtml = true;
                    mailMessage.To.Add(recipient);

                    await _smtpClient.SendMailAsync(mailMessage);
                }
            }
        }
    }
}







/*using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Threading.Tasks;

namespace ETMS.Services
{
    public class EmailService : IEmailService
    {
        public async Task SendEmailAsync(List<string> recipients, string subject, string body)
        {
            foreach (var recipient in recipients)
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress("your-email@example.com"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(recipient);
                await smtpClient.SendMailAsync(mailMessage);
            }
        }


    }
}
*/