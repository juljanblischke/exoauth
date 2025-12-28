using System.Text;
using System.Text.Json;
using ExoAuth.EmailWorker.Models;
using ExoAuth.EmailWorker.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ExoAuth.EmailWorker.Consumers;

public sealed class SendEmailConsumer : BackgroundService
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly IEmailTemplateService _templateService;
    private readonly ILogger<SendEmailConsumer> _logger;
    private readonly EmailSettings _emailSettings;

    private const string Exchange = "exoauth.events";
    private const string Queue = "email.send.queue";
    private const string RoutingKey = "email.send";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SendEmailConsumer(
        RabbitMqConnectionFactory connectionFactory,
        IEmailTemplateService templateService,
        IConfiguration configuration,
        ILogger<SendEmailConsumer> logger)
    {
        _connectionFactory = connectionFactory;
        _templateService = templateService;
        _logger = logger;

        _emailSettings = new EmailSettings();
        configuration.GetSection("Email").Bind(_emailSettings);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SendEmailConsumer starting...");

        try
        {
            var channel = await _connectionFactory.CreateChannelAsync(stoppingToken);

            // Declare exchange
            await channel.ExchangeDeclareAsync(
                exchange: Exchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken);

            // Declare queue
            await channel.QueueDeclareAsync(
                queue: Queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);

            // Bind queue to exchange
            await channel.QueueBindAsync(
                queue: Queue,
                exchange: Exchange,
                routingKey: RoutingKey,
                cancellationToken: stoppingToken);

            // Set prefetch count for fair dispatch
            await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var message = JsonSerializer.Deserialize<SendEmailMessage>(json, JsonOptions);

                    if (message is not null)
                    {
                        await ProcessEmailAsync(message, stoppingToken);
                        await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                        _logger.LogInformation("Email sent successfully to {To}", message.To);
                    }
                    else
                    {
                        _logger.LogWarning("Received null email message, acknowledging anyway");
                        await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process email message");
                    // Negative acknowledgment with requeue=false (send to dead letter queue if configured)
                    await channel.BasicNackAsync(ea.DeliveryTag, false, false, stoppingToken);
                }
            };

            await channel.BasicConsumeAsync(
                queue: Queue,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation("SendEmailConsumer is listening on queue {Queue}", Queue);

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SendEmailConsumer stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendEmailConsumer encountered an error");
        }
    }

    private async Task ProcessEmailAsync(SendEmailMessage message, CancellationToken ct)
    {
        // Render the email template
        var htmlBody = _templateService.Render(message.TemplateName, message.Variables, message.Language);

        // Create the email message
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
        mimeMessage.To.Add(MailboxAddress.Parse(message.To));
        mimeMessage.Subject = message.Subject;

        var builder = new BodyBuilder
        {
            HtmlBody = htmlBody
        };

        mimeMessage.Body = builder.ToMessageBody();

        // Send the email
        using var smtpClient = new SmtpClient();

        var secureSocketOptions = _emailSettings.SmtpUseSsl
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.None;

        await smtpClient.ConnectAsync(
            _emailSettings.SmtpHost,
            _emailSettings.SmtpPort,
            secureSocketOptions,
            ct);

        if (!string.IsNullOrEmpty(_emailSettings.SmtpUsername))
        {
            await smtpClient.AuthenticateAsync(
                _emailSettings.SmtpUsername,
                _emailSettings.SmtpPassword,
                ct);
        }

        await smtpClient.SendAsync(mimeMessage, ct);
        await smtpClient.DisconnectAsync(true, ct);
    }
}
