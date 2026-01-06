using System.Net;
using System.Net.Mail;

namespace Focus2Infinity.Services
{
  public interface IEMailService
  {
    Task SendAsync(
      string from,
      string subject,
      string htmlBody,
      CancellationToken ct = default);
  }

  public class EMailService (string server, string user, string pw, string to) : IEMailService
  {
    string _smtpServer = server;
    string _smtpUser = user;
    string _smtpPassword = pw;
    string _to = to;

    // Sends an email via SMTP using .NET 8 compatible APIs
    public async Task SendAsync(string from, string subject, string htmlBody, CancellationToken ct = default)
    {
      const int smtpPort = 25;
      const bool enableSsl = true;
      const int timeoutSeconds = 10;

      using var message = new MailMessage(from, _to)
      {
        Subject = subject,
        Body = htmlBody,
        IsBodyHtml = true
      };

      using var client = new SmtpClient(_smtpServer, smtpPort)
      {
        EnableSsl = enableSsl,
        DeliveryMethod = SmtpDeliveryMethod.Network,
        UseDefaultCredentials = false,
        Credentials = new NetworkCredential(_smtpUser, _smtpPassword),
        Timeout = timeoutSeconds * 1000
      };

      // SmtpClient does not provide true async send; wrap in Task.Run to avoid blocking the caller.
      await Task.Run(() => client.Send(message), ct);
    }
  }
}
