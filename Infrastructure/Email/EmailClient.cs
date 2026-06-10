using Microsoft.Extensions.Options;
using Polly;
using System.Net.Http.Json;

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
    private readonly EmailOptions _options;

    public EmailClient(HttpClient httpClient, IOptions<EmailOptions> options)
    {
        _httpClient = httpClient;
        _pipeline = EmailPolicies.RetryPipeline;
        _options = options.Value;
    }

    public async Task<HttpResponseMessage> SendEmailAsync(EmailSendPayload payload, CancellationToken ct)
        => await _pipeline.ExecuteAsync(
            static async (state, ct) =>
            {
                var (client, p) = state;
                return await client._httpClient.PostAsJsonAsync(client._options.SendEndpoint, p, ct);
            },
            (this, payload),
            ct);

}
