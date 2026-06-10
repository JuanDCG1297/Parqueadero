namespace Infrastructure.Email;

public record EmailSendPayload
{
    public ConfigEmail ConfigEmail { get; init; } = new();
    public ConfigParams ConfigParams { get; init; } = new();
    public Receivers Receivers { get; init; } = new();
    public EmailContent Email { get; init; } = new();
}

public record ConfigEmail
{
    public string Subject { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

public record ConfigParams
{
    public string IdUser { get; init; } = string.Empty;
    public string IdMessage { get; init; } = string.Empty;
}

public record Receivers
{
    public string EmailOrigen { get; init; } = string.Empty;
    public string[] To { get; init; } = Array.Empty<string>();
    public string[] CopyTo { get; init; } = Array.Empty<string>();
    public string[] HiddenCopyTo { get; init; } = Array.Empty<string>();
}

public record EmailContent
{
    public string Subject { get; init; } = string.Empty;
    public string UrlHeader { get; init; } = string.Empty;
    public string UrlFooter { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string[] UrlFiles { get; init; } = Array.Empty<string>();
}
