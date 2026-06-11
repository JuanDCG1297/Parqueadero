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

    /// <summary>
    /// Metodo que intercepta las solicitudes HTTP salientes hacia la API de envio de correo para agregar el token de autenticacion en el encabezado Authorization. Si la respuesta es 401 Unauthorized, intenta refrescar el token y reintentar la solicitud una vez. Utiliza un semaforo para evitar que múltiples solicitudes intenten refrescar el token al mismo tiempo.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email API no disponible ({Url}). El email no se enviará.", _options.BaseUrl);
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }
    }

    /// <summary>
    /// Metodo que genera el token, este metodo se llama siempre que se hace un llamado a la API de envio de correo para obtener el token del cache o generar uno nuevo si el token ha expirado. El token se almacena en cache por 55 minutos para evitar llamadas innecesarias a la API de autenticacion y mejorar el rendimiento.
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async Task<string> GetTokenAsync(CancellationToken ct)
    {
        if (_cache.TryGetValue("email_api_token", out string? token) && token is not null)
            return token;

        //Se utiliza el semaforo para evitar que múltiples solicitudes intenten refrescar el token al mismo tiempo, actualmente esta como maxima 1 solicitud
        await _semaphore.WaitAsync(ct);
        try
        {
            // Double-check after acquiring lock
            if (_cache.TryGetValue("email_api_token", out string? cachedToken) && cachedToken is not null)
                return cachedToken;

            token = await FetchTokenAsync(ct);
            //Queda 55 minutos en cache el token para mejorar rendimiento y evitar llamados constantes al Api
            _cache.Set("email_api_token", token, TimeSpan.FromMinutes(55));
            return token;
        }
        finally { _semaphore.Release(); }
    }

    /// <summary>
    /// Metodo que hace el llamado HTTP POST a la API de autenticacion para obtener un nuevo token de acceso utilizando las credenciales configuradas. Si la respuesta es exitosa, se extrae el token del cuerpo de la respuesta y se retorna. Si falla, se lanza una excepción.
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
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
