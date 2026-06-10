using System.Net.Http.Json;
using Polly;

namespace Infrastructure.Email;

/// <summary>
/// Typed HttpClient for email API.
/// Authentication header is injected by EmailDelegatingHandler.
/// Retry pipeline wraps the HTTP call with Polly exponential backoff.
/// </summary>
public class EmailClient
{
    private readonly HttpClient _httpClient;
    private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;

    public EmailClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _pipeline = EmailPolicies.RetryPipeline;
    }

    public async Task<HttpResponseMessage> SendEmailAsync(EmailSendPayload payload, CancellationToken ct)
        => await _pipeline.ExecuteAsync(
            static async (state, ct) =>
            {
                var (client, p) = state;
                return await client._httpClient.PostAsJsonAsync("/api/email/sendEmail", p, ct);
            },
            (this, payload),
            ct);
}
