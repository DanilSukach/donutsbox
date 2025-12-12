using Confluent.Kafka;
using File.Service.Api.Models;
using System.Text.Json;

namespace File.Service.Api.Services;

public class KafkaConsumerService(
    ILogger<KafkaConsumerService> logger, 
    IServiceProvider provider, 
    IConfiguration config,
    VideoCancellationService cancellationService) : BackgroundService
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
            MaxPollIntervalMs = 1800000, // 30 минут для длительных операций обработки видео
            // Оптимизация производительности
            FetchMinBytes = 1, // Минимальный размер батча для получения
            FetchWaitMaxMs = 500, // Максимальное время ожидания для накопления батча
            MaxPartitionFetchBytes = 1048576 // 1MB на партицию
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
                var cr = consumer.Consume(TimeSpan.FromSeconds(10));

                if (cr == null || cr.IsPartitionEOF)
                {
                    // Добавляем небольшую задержку при отсутствии сообщений для снижения нагрузки
                    await Task.Delay(100, stoppingToken);
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

                // Check if video processing was already cancelled
                if (cancellationService.IsCancelled(evt.VideoId))
                {
                    logger.LogInformation("Video {VideoId} was cancelled before processing started, skipping", evt.VideoId);
                    consumer.Commit(cr);
                    continue;
                }

                using var scope = provider.CreateScope();
                var ffmpeg = scope.ServiceProvider.GetRequiredService<FfmpegService>();
                var producer = scope.ServiceProvider.GetRequiredService<KafkaProducerService>();

                // Register video for cancellation tracking
                var cancellationToken = cancellationService.RegisterVideo(evt.VideoId);

                try
                {
                    logger.LogInformation("Starting video processing for {VideoId}", evt.VideoId);
                    var outputPath = await ffmpeg.ProcessVideoAsync(evt.VideoId, evt.ObjectKey, cancellationToken);

                    // Check cancellation again after processing
                    if (cancellationService.IsCancelled(evt.VideoId))
                    {
                        logger.LogInformation("Video {VideoId} was cancelled during processing", evt.VideoId);
                        consumer.Commit(cr);
                        continue;
                    }

                    logger.LogInformation("Video processed, publishing result for {VideoId}", evt.VideoId);
                    await producer.PublishProcessedAsync(new VideoProcessedEvent(evt.VideoId, outputPath));
                }
                finally
                {
                    cancellationService.UnregisterVideo(evt.VideoId);
                }

                consumer.Commit(cr);
                logger.LogInformation("Message committed successfully for offset {Offset}", cr.Offset);
            }
            catch (ConsumeException ex)
            {
                logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
            }
            catch (OperationCanceledException ex) when (!stoppingToken.IsCancellationRequested)
            {
                // Video processing was cancelled by user, not by service shutdown
                logger.LogInformation("Video processing was cancelled: {Message}", ex.Message);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Consumer cancelled");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing Kafka message");
                // Экспоненциальная задержка при ошибках, но не более 10 секунд
                await Task.Delay(1000, stoppingToken);
            }
        }

        logger.LogInformation("Kafka consumer shutting down");
        consumer.Close();
    }
}