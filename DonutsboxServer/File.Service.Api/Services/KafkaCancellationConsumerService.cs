using Confluent.Kafka;
using File.Service.Api.Models;
using System.Text.Json;

namespace File.Service.Api.Services;

public class KafkaCancellationConsumerService(
    ILogger<KafkaCancellationConsumerService> logger, 
    VideoCancellationService cancellationService, 
    IConfiguration config) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrapServers = config["Kafka:BootstrapServers"];
        var topic = config["Kafka:Topics:VideoProcessingCancelled"] ?? "video.processing.cancelled";
        var groupId = config["Kafka:GroupId"] + "-cancellation";

        logger.LogInformation("Initializing Kafka Cancellation Consumer: BootstrapServers={BootstrapServers}, Topic={Topic}, GroupId={GroupId}",
            bootstrapServers, topic, groupId);

        var conf = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            SessionTimeoutMs = 30000,
            AllowAutoCreateTopics = true,
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(conf)
            .SetErrorHandler((_, e) => logger.LogError("Kafka Cancellation Consumer Error: Code={Code}, Reason={Reason}",
                e.Code, e.Reason))
            .Build();

        consumer.Subscribe(topic);
        logger.LogInformation("Kafka cancellation consumer subscribed to topic {Topic}", topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cr = consumer.Consume(TimeSpan.FromSeconds(1));

                if (cr == null || cr.IsPartitionEOF)
                {
                    continue;
                }

                logger.LogInformation("Received cancellation message: {Message}", cr.Message.Value);

                var evt = JsonSerializer.Deserialize<VideoProcessingCancelledEvent>(cr.Message.Value, JsonOptions);

                if (evt != null)
                {
                    logger.LogInformation("Cancelling video processing for VideoId={VideoId}, Reason={Reason}",
                        evt.VideoId, evt.Reason);
                    cancellationService.CancelVideo(evt.VideoId, evt.Reason);
                }
            }
            catch (ConsumeException ex)
            {
                // Handle "Unknown topic" error gracefully - topic will be created when first message is sent
                if (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
                {
                    logger.LogDebug("Topic {Topic} not yet available, waiting...", topic);
                    await Task.Delay(5000, stoppingToken);
                }
                else
                {
                    logger.LogError(ex, "Kafka consume error in cancellation consumer: {Reason}", ex.Error.Reason);
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing cancellation message");
                await Task.Delay(500, stoppingToken);
            }
        }

        logger.LogInformation("Kafka cancellation consumer shutting down");
        consumer.Close();
    }
}

