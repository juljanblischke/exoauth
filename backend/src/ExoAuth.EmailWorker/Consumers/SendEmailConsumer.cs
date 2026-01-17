using System.Text;
using System.Text.Json;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.EmailWorker.Models;
using ExoAuth.EmailWorker.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ExoAuth.EmailWorker.Consumers;

public sealed class SendEmailConsumer : BackgroundService
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly Services.IEmailTemplateService _templateService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SendEmailConsumer> _logger;

    private const string Exchange = "exoauth.events";
    private const string Queue = "email.send.queue";
    private const string RoutingKey = "email.send";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SendEmailConsumer(
        RabbitMqConnectionFactory connectionFactory,
        Services.IEmailTemplateService templateService,
        IServiceScopeFactory scopeFactory,
        ILogger<SendEmailConsumer> logger)
    {
        _connectionFactory = connectionFactory;
        _templateService = templateService;
        _scopeFactory = scopeFactory;
        _logger = logger;
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
                        var result = await ProcessEmailAsync(message, stoppingToken);

                        if (result.Success)
                        {
                            await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                            _logger.LogInformation("Email sent successfully to {To} via provider {ProviderId}",
                                message.To, result.SentViaProviderId);
                        }
                        else
                        {
                            _logger.LogWarning("Email to {To} failed after all retries, moved to DLQ: {Error}",
                                message.To, result.Error);
                            // Acknowledge even on failure since it's been logged and moved to DLQ
                            await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                        }
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
                    // Negative acknowledgment with requeue=false (RabbitMQ dead letter queue)
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

    private async Task<EmailSendResult> ProcessEmailAsync(SendEmailMessage message, CancellationToken ct)
    {
        string htmlBody;
        string? plainTextBody;

        // If raw HTML is provided (announcements), use it directly
        // Otherwise, render from template
        if (!string.IsNullOrEmpty(message.HtmlBody))
        {
            htmlBody = message.HtmlBody;
            plainTextBody = message.PlainTextBody;
        }
        else
        {
            htmlBody = _templateService.Render(message.TemplateName, message.Variables, message.Language);
            plainTextBody = _templateService.RenderPlainText(message.TemplateName, message.Variables, message.Language);
        }

        // Serialize template variables for logging
        var templateVariablesJson = JsonSerializer.Serialize(message.Variables, JsonOptions);

        // Use scoped service for database access
        using var scope = _scopeFactory.CreateScope();
        var emailSendingService = scope.ServiceProvider.GetRequiredService<IEmailSendingService>();

        // Send with failover
        var result = await emailSendingService.SendWithFailoverAsync(
            recipientEmail: message.To,
            recipientUserId: message.RecipientUserId,
            subject: message.Subject,
            htmlBody: htmlBody,
            plainTextBody: plainTextBody,
            templateName: message.TemplateName,
            templateVariables: templateVariablesJson,
            language: message.Language,
            announcementId: message.AnnouncementId,
            existingEmailLogId: message.ExistingEmailLogId,
            cancellationToken: ct
        );

        return result;
    }
}
