namespace StartupIMS.Infrastructure.Services;

public interface IEmailService
{
    /// <summary>
    /// Sends an email. Implementations should never throw on failure to send -
    /// callers treat email as best-effort and must not have their own
    /// operation (registration, funding approval, etc.) fail because SMTP
    /// was unreachable. Log and swallow instead.
    /// </summary>
    Task SendAsync(string toAddress, string subject, string htmlBody);
}
