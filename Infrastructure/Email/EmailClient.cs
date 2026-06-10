using Microsoft.Extensions.Options;
using Polly;
using System.Net.Http.Json;

namespace Infrastructure.Email;

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

    /// <summary>
    /// Metodo que hace el llamado HTTP POST a la API de envio de correo con la informacion del correo a enviar. El pipeline de resiliencia maneja los reintentos mediante Polly.
    /// </summary>
    /// <param name="payload"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
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
