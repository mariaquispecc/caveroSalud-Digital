using System.Threading.Tasks;

namespace CaveroSalud.Infrastructure.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
    }
}
