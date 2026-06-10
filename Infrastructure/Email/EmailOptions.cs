namespace Infrastructure.Email;

public class EmailOptions
{
    public const string SectionName = "Email";
    public string BaseUrl { get; set; } = string.Empty;
    public string TokenEndpoint { get; set; } = "/api/token/";
    public string SendEndpoint { get; set; } = "/api/email/sendEmail";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int RetryCount { get; set; } = 3;
}
