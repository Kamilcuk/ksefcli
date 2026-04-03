using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace KCKSeFCli;

public static class SendMail {
    public static async Task Send(SmtpConfig config, string to, string subject, string body, List<string> attachments, CancellationToken cancellationToken) {
        MimeMessage message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(config.From));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        BodyBuilder builder = new BodyBuilder {
            TextBody = body
        };

        foreach (string filePath in attachments) {
            if (File.Exists(filePath)) {
                builder.Attachments.Add(filePath);
            }
        }

        message.Body = builder.ToMessageBody();

        using SmtpClient client = new SmtpClient();
        SecureSocketOptions options = config.UseSsl ? SecureSocketOptions.StartTlsWhenAvailable : SecureSocketOptions.None;
        if (config.Port == 465) options = SecureSocketOptions.SslOnConnect;

        await client.ConnectAsync(config.Host, config.Port, options, cancellationToken).ConfigureAwait(false);
        
        string password = config.GetPassword();
        if (!string.IsNullOrEmpty(config.User) && !string.IsNullOrEmpty(password)) {
            await client.AuthenticateAsync(config.User, password, cancellationToken).ConfigureAwait(false);
        }

        await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
        await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
    }
}
