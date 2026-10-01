using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using SportZone.Application.Interfaces.IService;
using SportZone.Infrastructure.Configuration;

namespace SportZone.Infrastructure.Service;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;

    // 1. Inject IOptions<EmailSettings> để lấy cấu hình từ appsettings.json
    public EmailService(IOptions<EmailSettings> options)
    {
        _settings = options.Value;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        // 2. Cấu hình máy chủ SMTP (ở đây là Google)
        var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_settings.Email, _settings.Password)
        };

        // 3. Tạo nội dung bức thư
        var mailMessage = new MailMessage
        {
            From = new MailAddress(_settings.Email, _settings.DisplayName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true // Cho phép nội dung mail dùng thẻ HTML (in đậm, màu sắc, link...)
        };
        
        mailMessage.To.Add(to);

        // 4. Gửi mail
        await client.SendMailAsync(mailMessage);
    }
}