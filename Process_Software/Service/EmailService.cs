using Process_Software.Helpter;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace Process_Software.Service
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings emailSettings;
        public EmailService(IOptions<EmailSettings> options)
        {
            this.emailSettings = options.Value;
        }
        public void SendEmail(Mailrequest mailrequest)
        {
            var email = new MimeMessage();
            email.Sender = MailboxAddress.Parse(emailSettings.Email);

            foreach (var toEmail in mailrequest.ToEmails)
            {
                email.To.Add(MailboxAddress.Parse(toEmail));
            }

            email.Subject = mailrequest.Subject;
            var builder = new BodyBuilder();

            builder.HtmlBody = mailrequest.Body;
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            smtp.Connect(emailSettings.Host, emailSettings.Port, SecureSocketOptions.StartTls);
            smtp.AuthenticationMechanisms.Remove("XOAUTH2");
            smtp.Authenticate(emailSettings.Email, emailSettings.Password);
            smtp.Send(email);
            smtp.Disconnect(true);
        }

    }
}