using Confluent.Kafka;
using File.Service.Api.Models;
using System.Text.Json;

namespace File.Service.Api.Services;

public class KafkaConsumerService(ILogger<KafkaConsumerService> logger, IServiceProvider provider, IConfiguration config) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrapServers = config["Kafka:BootstrapServers"];
        var topic = config["Kafka:Topics:VideoUploaded"];
        var groupId = config["Kafka:GroupId"];

        logger.LogInformation("Initializing Kafka Consumer: BootstrapServers={BootstrapServers}, Topic={Topic}, GroupId={GroupId}",
            bootstrapServers, topic, groupId);

        var conf = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            SessionTimeoutMs = 30000,
            MaxPollIntervalMs = 300000
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(conf)
            .SetErrorHandler((_, e) => logger.LogError("Kafka Consumer Error: Code={Code}, Reason={Reason}, IsFatal={IsFatal}",
                e.Code, e.Reason, e.IsFatal))
            .Build();

        consumer.Subscribe(topic);
        logger.LogInformation("Kafka consumer subscribed to topic {Topic}", topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cr = consumer.Consume(TimeSpan.FromSeconds(5));

                if (cr == null || cr.IsPartitionEOF)
                {
                    continue;
                }

                logger.LogInformation("Received message from partition {Partition}, offset {Offset}: {Message}",
                    cr.Partition, cr.Offset, cr.Message.Value);

                var evt = JsonSerializer.Deserialize<VideoUploadedEvent>(cr.Message.Value, JsonOptions);

                if (evt == null)
                {
                    logger.LogWarning("Failed to deserialize message, skipping");
                    consumer.Commit(cr);
                    continue;
                }

                logger.LogInformation("Processing video.uploaded event for VideoId={VideoId}, ObjectKey={ObjectKey}",
                    evt.VideoId, evt.ObjectKey);

                using var scope = provider.CreateScope();
                var ffmpeg = scope.ServiceProvider.GetRequiredService<FfmpegService>();
                var producer = scope.ServiceProvider.GetRequiredService<KafkaProducerService>();

                logger.LogInformation("Starting video processing for {VideoId}", evt.VideoId);
                var outputPath = await ffmpeg.ProcessVideoAsync(evt.VideoId, evt.ObjectKey);

                logger.LogInformation("Video processed, publishing result for {VideoId}", evt.VideoId);
                await producer.PublishProcessedAsync(new VideoProcessedEvent(evt.VideoId, outputPath));

                consumer.Commit(cr);
                logger.LogInformation("Message committed successfully for offset {Offset}", cr.Offset);
            }
            catch (ConsumeException ex)
            {
                logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Consumer cancelled");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing Kafka message");
                await Task.Delay(1000, stoppingToken);
            }
        }

        logger.LogInformation("Kafka consumer shutting down");
        consumer.Close();
    }
}