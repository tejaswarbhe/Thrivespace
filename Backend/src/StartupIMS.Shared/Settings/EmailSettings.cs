namespace StartupIMS.Shared.Settings;

public class EmailSettings
{
    public const string SectionName = "Email";

    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public bool UseSsl { get; set; } = true;

    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "StartupIMS";

    // Lets you disable actually sending mail in local dev without commenting
    // out code - emails just get logged instead.
    public bool Enabled { get; set; } = true;
}
