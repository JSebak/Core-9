using Business.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string recipientEmail, string subject, string message)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            _logger.LogWarning("Attempted to send an email with an invalid recipient email.");
            throw new ArgumentException("Recipient email is required.", nameof(recipientEmail));
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            _logger.LogWarning("Attempted to send an email with an empty subject.");
            throw new ArgumentException("Subject is required.", nameof(subject));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            _logger.LogWarning("Attempted to send an email with an empty message.");
            throw new ArgumentException("Message body is required.", nameof(message));
        }

        var emailMessage = new MimeMessage();
        try
        {
            emailMessage.From.Add(new MailboxAddress(_configuration["SmtpSettings:SenderName"], _configuration["SmtpSettings:SenderEmail"]));
            emailMessage.To.Add(new MailboxAddress("", recipientEmail));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart("html") { Text = message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the email message.");
            throw new Exception("An error occurred while preparing the email. Please check the email details and try again.", ex);
        }

        using (var client = new SmtpClient())
        {
            try
            {
                await client.ConnectAsync(_configuration["SmtpSettings:Server"], int.Parse(_configuration["SmtpSettings:Port"]), false);
                await client.AuthenticateAsync(_configuration["SmtpSettings:Username"], _configuration["SmtpSettings:Password"]);
                await client.SendAsync(emailMessage);
            }
            catch (SmtpCommandException ex)
            {
                _logger.LogError(ex, "SMTP command error while sending an email to {RecipientEmail}.", recipientEmail);
                throw new Exception("An SMTP error occurred while sending the email. Please try again later.", ex);
            }
            catch (SmtpProtocolException ex)
            {
                _logger.LogError(ex, "SMTP protocol error while sending an email to {RecipientEmail}.", recipientEmail);
                throw new Exception("An SMTP protocol error occurred while sending the email. Please try again later.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while sending an email to {RecipientEmail}.", recipientEmail);
                throw new Exception("An unexpected error occurred while sending the email. Please try again later.", ex);
            }
            finally
            {
                try
                {
                    await client.DisconnectAsync(true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "An error occurred while disconnecting from the SMTP server.");
                }
            }
        }
    }
}
