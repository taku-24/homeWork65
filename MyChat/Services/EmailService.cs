using MailKit.Net.Smtp;
using MimeKit;

namespace MyChat.Services
{
    public class EmailService
    {
        public async Task SendAsync(string email, string subject, string message)
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress("MyChat", "...gmail.com"));
            msg.To.Add(new MailboxAddress("", email));
            msg.Subject = subject;
            msg.Body = new TextPart("html") { Text = message };

            using var client = new SmtpClient();
            await client.ConnectAsync("smtp.gmail.com", 465, true);
            await client.AuthenticateAsync("...gmail.com", "...");
            await client.SendAsync(msg);
            await client.DisconnectAsync(true);
        }
    }
}