using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBackend.Services.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task SendEmail(string email, string subject, string body)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("StayHub", _configuration["EmailSettings:SenderEmail"]));
            emailMessage.To.Add(new MailboxAddress("", email));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart("html") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(_configuration["EmailSettings:SmtpServer"],
                                      int.Parse(_configuration["EmailSettings:Port"]),
                                      SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_configuration["EmailSettings:SenderEmail"],
                                           _configuration["EmailSettings:Password"]);
            await client.SendAsync(emailMessage);
            await client.DisconnectAsync(true);
        }
        public async Task SendOtpEmail(string email, string otp)
        {
            var templatePath = "/Users/tungld/Documents/Code/sp/HotelManagement/Backend/HotelManagementSystem_BE/AppBackend.Services/TemplateEmail/OtpEmailTemplate.html";
            string template;
            using (var reader = new System.IO.StreamReader(templatePath))
            {
                template = await reader.ReadToEndAsync();
            }
            var body = template.Replace("{{OTP}}", otp);
            await SendEmail(email, "Your OTP Code", body);
        }
    }
}
