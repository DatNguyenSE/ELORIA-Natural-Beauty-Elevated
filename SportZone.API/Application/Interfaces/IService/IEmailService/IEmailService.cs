namespace SportZone.Application.Interfaces.IService;
public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
}