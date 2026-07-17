using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net;

namespace CaveroSalud.Infrastructure.Services
{
    public class SmtpOptions
    {
        public string Host { get; set; }
        public int Port { get; set; } = 25;
        public bool UseSsl { get; set; } = false;
        public string Username { get; set; }
        public string Password { get; set; }
        public string From { get; set; }
    }

    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpOptions _options;

        public SmtpEmailSender(IOptions<SmtpOptions> options)
        {
            _options = options.Value;
        }

        public Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var msg = new MailMessage();
            msg.From = new MailAddress(_options.From);
            msg.To.Add(new MailAddress(toEmail));
            msg.Subject = subject;
            msg.Body = htmlMessage;
            msg.IsBodyHtml = true;

            var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.UseSsl,
            };

            if (!string.IsNullOrEmpty(_options.Username))
            {
                client.Credentials = new NetworkCredential(_options.Username, _options.Password);
            }

            return client.SendMailAsync(msg);
        }
    }
}
