using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Email;

public class EmailDelegatingHandler : DelegatingHandler
{
    private readonly IMemoryCache _cache;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly EmailOptions _options;
    private readonly ILogger<EmailDelegatingHandler> _logger;

    public EmailDelegatingHandler(
        IMemoryCache cache,
        IOptions<EmailOptions> options,
        ILogger<EmailDelegatingHandler> logger)
    {
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = await GetTokenAsync(ct);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await base.SendAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Email API token expired, refreshing...");
            await _semaphore.WaitAsync(ct);
            try
            {
                // Another request might have refreshed it while we waited
                if (_cache.TryGetValue("email_api_token", out string? cachedToken) && cachedToken is not null)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cachedToken);
                }
                else
                {
                    token = await FetchTokenAsync(ct);
                    _cache.Set("email_api_token", token, TimeSpan.FromMinutes(55));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
                return await base.SendAsync(request, ct);
            }
            finally { _semaphore.Release(); }
        }

        return response;
    }

    private async Task<string> GetTokenAsync(CancellationToken ct)
    {
        if (_cache.TryGetValue("email_api_token", out string? token) && token is not null)
            return token;

        await _semaphore.WaitAsync(ct);
        try
        {
            // Double-check after acquiring lock
            if (_cache.TryGetValue("email_api_token", out string? cachedToken) && cachedToken is not null)
                return cachedToken;

            token = await FetchTokenAsync(ct);
            _cache.Set("email_api_token", token, TimeSpan.FromMinutes(55));
            return token;
        }
        finally { _semaphore.Release(); }
    }

    private async Task<string> FetchTokenAsync(CancellationToken ct)
    {
        var tokenRequest = new { username = _options.Username, password = _options.Password };
        var tokenUrl = new Uri(new Uri(_options.BaseUrl), _options.TokenEndpoint);

        var response = await base.SendAsync(
            new HttpRequestMessage(HttpMethod.Post, tokenUrl)
            {
                Content = JsonContent.Create(tokenRequest)
            }, ct);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<TokenResponse>(ct);
        return result?.Token ?? throw new InvalidOperationException("Failed to get email API token");
    }

    private record TokenResponse(int Code, string? Message, string Token);
}
