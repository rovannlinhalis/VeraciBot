using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using VeraciBot.Application.Services;
using VeraciBot.Core.Entities;

namespace VeraciBot.App.Components.Account
{
    internal sealed class IdentitySmtpEmailSender(ApplicationSettingsService settingsService, ILogger<IdentitySmtpEmailSender> logger) : IEmailSender<ApplicationUser>
    {
        public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
            SendEmailAsync(
                email,
                "Confirme seu e-mail",
                $"Por favor, confirme sua conta clicando <a href='{confirmationLink}'>aqui</a>."
            );

        public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
            SendEmailAsync(
                email,
                "Redefina sua senha",
                $"Para redefinir sua senha, clique <a href='{resetLink}'>aqui</a>."
            );

        public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
            SendEmailAsync(
                email,
                "Código para redefinição de senha",
                $"Use o código a seguir para redefinir sua senha: <strong>{WebUtility.HtmlEncode(resetCode)}</strong>."
            );

        private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var enabled = ParseYesNo(await settingsService.GetValueAsync(ApplicationParameter.SMTP_ENABLED), defaultValue: false);
            if (!enabled)
            {
                logger.LogInformation("SMTP desabilitado. E-mail para {Email} não foi enviado.", toEmail);
                return;
            }

            var host = (await settingsService.GetValueAsync(ApplicationParameter.SMTP_HOST) ?? string.Empty).Trim();
            var fromEmail = (await settingsService.GetValueAsync(ApplicationParameter.SMTP_FROM_EMAIL) ?? string.Empty).Trim();
            var fromName = (await settingsService.GetValueAsync(ApplicationParameter.SMTP_FROM_NAME) ?? "VeraciBot").Trim();
            var username = (await settingsService.GetValueAsync(ApplicationParameter.SMTP_USERNAME) ?? string.Empty).Trim();
            var password = await settingsService.GetValueAsync(ApplicationParameter.SMTP_PASSWORD) ?? string.Empty;
            var port = ParseInt(await settingsService.GetValueAsync(ApplicationParameter.SMTP_PORT), 587);
            var enableSsl = ParseYesNo(await settingsService.GetValueAsync(ApplicationParameter.SMTP_ENABLE_SSL), defaultValue: true);

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromEmail))
            {
                logger.LogWarning("SMTP está habilitado, mas Host/FromEmail não foram configurados. E-mail para {Email} não foi enviado.", toEmail);
                return;
            }

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(new MailAddress(toEmail));

            using var smtp = new SmtpClient(host, Math.Clamp(port, 1, 65535))
            {
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            if (!string.IsNullOrWhiteSpace(username))
            {
                smtp.Credentials = new NetworkCredential(username, password);
            }

            await smtp.SendMailAsync(message);
        }

        private static bool ParseYesNo(string raw, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return defaultValue;
            }

            var normalized = raw.Trim().ToLowerInvariant();
            return normalized switch
            {
                "1" => true,
                "0" => false,
                "true" => true,
                "false" => false,
                "yes" => true,
                "no" => false,
                _ => defaultValue
            };
        }

        private static int ParseInt(string raw, int defaultValue)
        {
            return int.TryParse(raw, out var value) ? value : defaultValue;
        }
    }
}
