using Domain.Entities;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Email;

public class EmailService : IEmailService
{
    private readonly EmailClient _emailClient;
    private readonly ILogger<EmailService> _logger;
    private readonly EmailOptions _options;

    public EmailService(EmailClient emailClient, ILogger<EmailService> logger, IOptions<EmailOptions> options)
    {
        _emailClient = emailClient;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Metodo que genera la informacion que se envia por correo al cliente cuando el vehiculo sale
    /// </summary>
    /// <param name="entry"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task SendExitNotificationAsync(VehicleEntry entry, CancellationToken ct = default)
    {
        try
        {
            var payload = new EmailSendPayload
            {
                ConfigEmail = new ConfigEmail
                {
                    Message = "notification",
                    Subject = "Salida de vehículo registrada"
                },
                ConfigParams = new ConfigParams
                {
                    IdUser = _options.Username,
                    IdMessage = "exit_notification"
                },
                Receivers = new Receivers
                {
                    EmailOrigen = "cruzcf366@gmail.com",
                    To = new[] { "cruzcf1297@gmail.com" }
                },
                Email = new EmailContent
                {
                    Subject = $"Salida de vehículo - {entry.Plate}",
                    Message = $@"
                        <h1>Salida Registrada</h1>
                        <p><strong>Placa:</strong> {entry.Plate}</p>
                        <p><strong>Tipo:</strong> {entry.VehicleType}</p>
                        <p><strong>Ingreso:</strong> {entry.EntryTime:dd/MM/yyyy HH:mm}</p>
                        <p><strong>Salida:</strong> {entry.ExitTime:dd/MM/yyyy HH:mm}</p>
                        <p><strong>Tiempo total:</strong> {entry.TotalMinutes} minutos</p>
                        <p><strong>Valor pagado:</strong> ${entry.Fee:N2} COP</p>
                    ",
                    UrlHeader = string.Empty,
                    UrlFooter = string.Empty,
                    UrlFiles = Array.Empty<string>()
                }
            };

            //Llamado a la API de envio de correo
            var response = await _emailClient.SendEmailAsync(payload, ct);
            if (response.IsSuccessStatusCode)
            {
                entry.MarkEmailSent();
                _logger.LogInformation("Email enviado de manera exitosa {Plate}", entry.Plate);
            }
            else
            {
                _logger.LogWarning("Email API responde con {StatusCode} para el vehiculo {Plate}", response.StatusCode, entry.Plate);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fallo el envio del Email para el vehiculo {Plate}.", entry.Plate);
        }
    }
}
